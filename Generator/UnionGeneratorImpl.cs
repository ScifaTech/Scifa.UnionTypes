using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace UnionTypes.Generator
{
    internal class UnionGeneratorImpl
    {
        public static bool TryGetInstance(Compilation compilation, [NotNullWhen(true)] out UnionGeneratorImpl? generator)
        {
            var namedTypeAttributeSymbol = compilation.GetTypeByMetadataName("Scifa.UnionTypes.UnionTypeAttribute");
            if (namedTypeAttributeSymbol is null)
            {
                generator = default;
                return false;
            }
            else
            {
                generator = new(compilation, namedTypeAttributeSymbol);
                return true;
            }
        }

        private Compilation _compilation;
        private readonly INamedTypeSymbol? _namedTypeAttributeSymbol;

        private UnionGeneratorImpl(Compilation compilation, INamedTypeSymbol namedTypeAttributeSymbol)
        {
            _compilation = compilation;
            _namedTypeAttributeSymbol = namedTypeAttributeSymbol;
        }

        internal bool TryGetSource(TypeDeclarationSyntax typeDeclaration, [NotNullWhen(true)] out string? hintName, [NotNullWhen(true)] out SourceText? source)
        {
            var model = _compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
            if (IsUnionType(model, typeDeclaration, out var typeSymbol))
            {
                hintName = GetHintName(typeSymbol);
                source = GenerateSource(model, typeSymbol);
                return true;
            }
            else
            {
                hintName = default;
                source = default;
                return false;
            }
        }

        private bool IsUnionType(SemanticModel model, TypeDeclarationSyntax typeDeclaration, [NotNullWhen(true)] out INamedTypeSymbol? declarationSymbol)
        {
            declarationSymbol = model.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
            if (declarationSymbol is null)
                return false;

            if (declarationSymbol.IsStatic || declarationSymbol.IsAnonymousType || declarationSymbol.IsTupleType)
                return false;

            return declarationSymbol
                    .GetAttributes()
                    .Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _namedTypeAttributeSymbol))
                    .Any();
        }

        private string GetHintName(INamedTypeSymbol typeSymbol) => typeSymbol.MetadataName + ".cs";

        private SourceText GenerateSource(SemanticModel model, INamedTypeSymbol typeSymbol)
        {
            var typeNesting = typeSymbol.Unfold(x => x.ContainingType).Reverse().Append(typeSymbol).ToArray();
            var declaredNamespace = typeNesting.First().ContainingNamespace.GetFullName();
            return SourceText.From($$"""
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                namespace {{declaredNamespace}};

                {{typeNesting.Take(typeNesting.Length - 1).Select(GetTypeHeader).Join("\n")}}
                [System.Diagnostics.DebuggerDisplay("{DebugView()}")]
                {{GetTypeHeader(typeNesting.Last())}}
                
                {{GetUnionTypeBody(model, typeSymbol)}}                
                {{GetTypeClosers(typeNesting)}}
                """.NormalizeLineEndings(),
                Encoding.UTF8);
        }

        private string GetUnionTypeBody(SemanticModel model, INamedTypeSymbol typeSymbol)
        {
            var cases = GetCases(typeSymbol);

            return $$"""
                {{CaseConstructors(typeSymbol, cases)}}

                private readonly CaseName __case;
                private readonly object __caseData;
            
                private {{typeSymbol.Name}}(CaseName @case, object caseData){
                    __case = @case;
                    __caseData = caseData;
                }

                public CaseName Case => __case;

            {{MatchFunctions(cases)}}
            
            {{DoFunctions(cases)}}
            
                private string DebugView()
                    => __case switch
                    {
                        {{cases.Select(@case =>
                            $$"""{{@case.CaseReference}} => $"{{@case.PascalName}}({{@case.Parameters.Select(param => $"{{{@case.ValueAccessExpression(param)}}}").Join(", ")}})","""
                          ).Join("\n            ")}}
                        var x => $"Invalid case {x}: {__caseData ?? "<<null>>"}"
                    };

                [MethodImpl(MethodImplOptions.NoInlining)]
                private static TOut ThrowInvalidCaseException<TOut>(CaseName @case)
                {
                    throw new InvalidOperationException($"Invalid state achieved for union of type {{typeSymbol.Name}}: case {@case} does not exist");
                }

                public enum CaseName
                {
                    {{cases.Select(cases => $"{cases.PascalName} = {cases.Index}").Join(",\n        ")}}
                }
            {{CaseRecords(cases)}}
            """;
        }

        private string MatchFunctions(UnionCase[] cases)
            => $$"""
                /// <summary>
                /// Given a function to excute for each union case, this method will select the the function for the the current case and execute that function providing the case arguments as function arguments.
                /// </summary>
                {{cases.Select(@case => $$"""/// <param name="{{@case.CamelName}}">The function to execute if this instance is a case of `{{@case.PascalName}}`.</param>""").Join("\n    ")}}
                /// <returns>The result of the given function for the current case.</returns>
                public TOut Match<TOut>({{cases.Select(
                            @case => $"Func<{@case.Parameters.Select(param => param.TypeName).Append("TOut").Join(",")}> {@case.CamelName.EscapeIfKeyword()}"
                        )
                        .Join(", ")}})
                    => __case switch
                    {
                        {{cases.Select(@case =>
                                           $$"""{{@case.CaseReference}} => {{@case.CamelName.EscapeIfKeyword()}}({{@case.Parameters.Select(param => @case.ValueAccessExpression(param)).Join(", ")}}),"""
                          ).Join("\n            ")}}
                        var x => ThrowInvalidCaseException<TOut>(x)
                    };
                
                /// <summary>
                /// Given a function to excute for some union cases and an `otherwise` function, this method will select the the function for the the current case and execute that function providing the case arguments as function arguments.
                /// </summary>
                {{cases.Select(@case => $$"""/// <param name="{{@case.CamelName}}">The function to execute if this instance is a case of `{{@case.PascalName}}`.</param>""").Join("\n    ")}}
                /// <param name="otherwise">The function to execute if this instance represents a case with no specific handler.</param>
                /// <returns>The result of the given function for the current case or the result of the `otherwise` function if no specific function was given.</returns>
                public TOut Match<TOut>({{cases.Select(
                            @case => $"Func<{@case.Parameters.Select(param => param.TypeName).Append("TOut").Join(",")}>? {@case.CamelName.EscapeIfKeyword()} = null"
                        )
                        .Prepend("Func<TOut> otherwise")
                        .Join(", ")}})
                    => __case switch
                    {
                        {{cases.Select(
                            @case =>
                                $$"""{{@case.CaseReference}} when {{@case.CamelName.EscapeIfKeyword()}} is not null => {{@case.CamelName.EscapeIfKeyword()}}({{@case.Parameters.Select(param => @case.ValueAccessExpression(param)).Join(", ")}}),""").Join("\n            ")}}
                        _ => otherwise()
                    };
            """;

        private object DoFunctions(UnionCase[] cases)
            => $$"""
                /// <summary>
                /// Given an action to excute for each union case, this method will select the the action for the the current case and execute that action providing the case arguments as action arguments.
                /// </summary>
                {{cases.Select(@case => $$"""/// <param name="{{@case.CamelName}}">The action to execute if this instance is a case of `{{@case.PascalName}}`.</param>""").Join("\n    ")}}
                public void Do({{(cases.Select(
                        @case =>
                            @case.Parameters switch
                            {
                                { Length: 0 } => $"Action {@case.CamelName.EscapeIfKeyword()}",
                                _ => $"Action<{@case.Parameters.Select(param => param.TypeName).Join(",")}> {@case.CamelName.EscapeIfKeyword()}"
                            }
                    ).Join(", "))}})
                {
                    switch (__case)
                    {
                        {{cases.Select(@case => $$"""case {{@case.CaseReference}}: {{@case.CamelName.EscapeIfKeyword()}}({{@case.Parameters.Select(param => @case.ValueAccessExpression(param)).Join(", ")}}); break;"""
                          ).Join("\n            ")}}
                        default: ThrowInvalidCaseException<byte>(__case); break;
                    }
                }
            
                /// <summary>
                /// Given an action to excute for some union cases and an `otherwise` action, this method will select the the action for the the current case and execute that action providing the case arguments as action arguments.
                /// </summary>
                {{cases.Select(@case => $$"""/// <param name="{{@case.CamelName}}">The action to execute if this instance is a case of `{{@case.PascalName}}`.</param>""").Join("\n    ")}}
                /// <param name="otherwise">The action to execute if this instance represents a case with no specific handler.</param>
                public void Do({{(cases.Select(
                        @case =>
                            @case.Parameters switch
                            {
                                { Length: 0 } => $"Action {@case.CamelName.EscapeIfKeyword()}",
                                _ => $"Action<{@case.Parameters.Select(param => param.TypeName).Join(",")}> {@case.CamelName.EscapeIfKeyword()}"
                            }
                    )
                    .Prepend("Action otherwise")
                    .Join(", "))}})
                {
                    switch (__case)
                    {
                        {{cases.Select(@case => $$"""case {{@case.CaseReference}}: {{@case.CamelName.EscapeIfKeyword()}}({{@case.Parameters.Select(param => @case.ValueAccessExpression(param)).Join(", ")}}); break;"""
                          ).Join("\n            ")}}
                        default: otherwise(); break;
                    }
                }
            """;

        private static UnionCase[] GetCases(INamedTypeSymbol typeSymbol)
        {
            var unorderedCases = (from method in typeSymbol.GetMembers().OfType<IMethodSymbol>()
                                  where method.IsPartialDefinition
                                      && !method.ReturnsVoid
                                      && !method.IsGenericMethod
                                  where SymbolEqualityComparer.Default.Equals(method.ReturnType, typeSymbol)
                                  select new UnionCase(0, typeSymbol, method)
                                 )
                                 .ToArray();

            var possibleDefaults = unorderedCases.Where(x => x.Parameters.Length == 0);
            if (possibleDefaults.Any() && !possibleDefaults.Skip(1).Any())
                possibleDefaults.First().IsDefault = true;

            var cases = unorderedCases.OrderBy(x => x.IsDefault ? 0 : 1).Indexed().Select(x => x.value with { Index = x.index }).ToArray();
            return cases;
        }

        private string GetTypeHeaders(INamedTypeSymbol[] typeNesting) => typeNesting.Select(GetTypeHeader).Join("\n");
        private string GetTypeHeader(INamedTypeSymbol type)
        {
            string accessibility = type.DeclaredAccessibility.GetSourceText();
            string typeKeywords = type switch
            {
                { IsRecord: true, IsValueType: true, IsReadOnly: true, IsRefLikeType: true } => "readonly ref partial record struct",
                { IsRecord: true, IsValueType: true, IsReadOnly: true } => "readonly partial record struct",
                { IsRecord: true, IsValueType: true } => "partial record struct",
                { IsRecord: true } => "partial record",

                { IsRecord: false, IsValueType: true, IsReadOnly: true, IsRefLikeType: true } => "readonly ref partial struct",
                { IsRecord: false, IsValueType: true, IsReadOnly: true } => "readonly partial struct",
                { IsRecord: false, IsValueType: true } => "partial struct",
                { IsRecord: false } => "partial class",
            };
            string typeName = type.GetLocalName();

            return $"{accessibility} {typeKeywords} {typeName} {{";
        }

        private string CaseConstructors(INamedTypeSymbol type, UnionCase[] cases)
            => cases.Select(@case => CaseConstructor(type, @case)).Join("\n\n    ");
        private string CaseConstructor(INamedTypeSymbol type, UnionCase @case)
        {
            string typeName = type.GetLocalName();
            string signature = $$"""{{@case.Accessibility}} static partial {{typeName}} {{@case.PascalName}}({{@case.Parameters.Select(param => $$"""{{param.TypeName}} {{param.RawName}}""").Join(", ")}}) => """;

            var body = @case switch
            {
                { IsDefault: true } => "default",
                { EmitCaseType: true } => $$"""new {{typeName}}({{@case.CaseReference}}, new {{@case.TypeName}}({{@case.Parameters.Select(param => param.CamelName.EscapeIfKeyword()).Join(", ")}}) )""",
                _ => $$"""new {{typeName}}({{@case.CaseReference}}, {{@case.Parameters[0].CamelName.EscapeIfKeyword()}})"""
            };

            return $"{signature}{body};";
        }

        private string CaseRecords(UnionCase[] cases)
            => cases.Where(x => x.EmitCaseType)
                .Select(@case =>
                    $$"""private record struct {{@case.TypeName}}({{@case.Parameters.Select(param => $$"""{{param.TypeName}} {{param.PascalName}}""").Join(", ")}});"""
                ).Join("\n    ");

        private string GetTypeClosers(INamedTypeSymbol[] typeNesting) => new string('}', typeNesting.Length);
    }

    internal record UnionCase(int Index, INamedTypeSymbol TypeSymbol, IMethodSymbol MethodSymbol)
    {
        public object Accessibility { get; } = MethodSymbol.DeclaredAccessibility.GetSourceText();
        public string PascalName { get; } = MethodSymbol.Name.ToPascalCase();
        public string CamelName { get; } = MethodSymbol.Name.ToCamelCase();
        public bool EmitCaseType { get; } = MethodSymbol.Parameters.Count() > 1;
        public string CaseReference { get; } = $"CaseName.{MethodSymbol.Name.ToPascalCase()}";
        public UnionCaseParameter[] Parameters { get; } = MethodSymbol.Parameters.Select(param => new UnionCaseParameter(param)).ToArray();
        public bool IsDefault { get; internal set; }
        public object TypeName => EmitCaseType
            ? $"{PascalName}CaseData"
            : MethodSymbol.Parameters.Any()
                ? MethodSymbol.Parameters[0].Type
                : "Unit";

        public string ValueAccessExpression(UnionCaseParameter param)
            => this.EmitCaseType ? $"(({this.TypeName})__caseData).{param.PascalName}" : $"(({this.TypeName})__caseData)";
    }

    internal record UnionCaseParameter(IParameterSymbol ParameterSymbol)
    {
        public string RawName { get; } = ParameterSymbol.Name;
        public string PascalName { get; } = ParameterSymbol.Name.ToPascalCase();
        public string CamelName { get; } = ParameterSymbol.Name.ToCamelCase();
        public string TypeName { get; } = ParameterSymbol.Type.GetFullName() + GetNullableAnnotation(ParameterSymbol);

        private static string GetNullableAnnotation(IParameterSymbol param)
            => param switch
            {
                { Type.IsValueType: false, NullableAnnotation: NullableAnnotation.Annotated } => "?",
                _ => string.Empty
            };
    }
}