﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;

namespace UnionTypes.Generator
{
    internal class UnionGeneratorImpl
    {
        public static bool TryGetInstance(Compilation compilation, [NotNullWhen(true)] out UnionGeneratorImpl? generator)
        {
            var namedTypeAttributeSymbol = compilation.GetTypeByMetadataName("UnionTypes.UnionTypeAttribute");
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

                {{GetTypeHeaders(typeNesting)}}
                
                {{GetUnionTypeBody(model, typeSymbol)}}
                
                {{GetTypeClosers(typeNesting)}}
                """,
                Encoding.UTF8);
        }

        private string GetUnionTypeBody(SemanticModel model, INamedTypeSymbol typeSymbol)
        {
            var cases = GetCases(typeSymbol);

            return $$"""
                {{CaseConstructors(typeSymbol, cases)}}

                private readonly int __case;
                private readonly object __caseData;
            
                
                private {{typeSymbol.Name}}(int @case, object caseData){
                    __case = @case;
                    __caseData = caseData;
                }

                {{MatchFunctions(cases)}}
            
                {{DoFunctions(cases)}}
            
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static TOut ThrowInvalidCaseException<TOut>(int @case)
                {
                    throw new InvalidOperationException($"Invalid state achieved for union of type {{typeSymbol.Name}}: case {@case} does not exist");
                }

                {{CaseRecords(cases)}}
            """;
        }

        private object MatchFunctions(UnionCase[] cases)
            => $$"""
                public TOut Match<TOut>({{cases.Select(
                            @case => $"Func<{@case.Parameters.Select(param => param.TypeName).Append("TOut").Join(",")}> {@case.CamelName}"
                        )
                        .Join(", ")}})
                    => __case switch
                    {
                        {{cases.Select(@case =>
                                           $$"""{{@case.Index}} => {{@case.CamelName}}({{@case.Parameters.Select(arg => $"(({@case.PascalName}CaseData)__caseData).{arg.PascalName}").Join(", ")}}),"""
                          ).Join("\n            ")}}
                        var x => ThrowInvalidCaseException<TOut>(x)
                    };
                    
                public TOut Match<TOut>({{cases.Select(
                            @case => $"Func<{@case.Parameters.Select(param => param.TypeName).Append("TOut").Join(",")}>? {@case.CamelName} = null"
                        )
                        .Prepend("Func<TOut> otherwise")
                        .Join(", ")}})
                    => __case switch
                    {
                        {{cases.Select(
                            @case =>
                                $$"""{{@case.Index}} when {{@case.CamelName}} is not null => {{@case.CamelName}}({{@case.Parameters.Select(arg => $"(({@case.PascalName}CaseData)__caseData).{arg.PascalName}").Join(", ")}}),""").Join("\n            ")}}
                        _ => otherwise()
                    };
                    
                public TOut Match<TOut>({{cases.Select(
                            @case => $"Func<{@case.Parameters.Select(param => param.TypeName).Append("TOut").Join(",")}>? {@case.CamelName} = null"
                        )
                        .Prepend("TOut otherwise")
                        .Join(", ")}})
                    => __case switch
                    {
                        {{cases.Select(
                            @case =>
                                $$"""{{@case.Index}} when {{@case.CamelName}} is not null => {{@case.CamelName}}({{@case.Parameters.Select(arg => $"(({@case.PascalName}CaseData)__caseData).{arg.PascalName}").Join(", ")}}),""").Join("\n            ")}}
                        _ => otherwise
                    };
            """;

        private object DoFunctions(UnionCase[] cases)
            => $$"""
                public void Do({{(cases.Select(
                        @case =>
                            @case.Parameters switch
                            {
                                { Length: 0 } => $"Action {@case.CamelName}",
                                _ => $"Action<{@case.Parameters.Select(param => param.TypeName).Join(",")}> {@case.CamelName}"
                            }
                    ).Join(", "))}})
                {
                    switch (__case)
                    {
                        {{cases.Select(@case =>
                                             $$"""case {{@case.Index}}: {{@case.CamelName}}({{@case.Parameters.Select(arg => $"(({@case.PascalName}CaseData)__caseData).{arg.PascalName}").Join(", ")}}); break;"""
                          ).Join("\n            ")}}
                        default: ThrowInvalidCaseException<byte>(__case); break;
                    }
                }
            
                public void Do({{(cases.Select(
                        @case =>
                            @case.Parameters switch
                            {
                                { Length: 0 } => $"Action {@case.CamelName}",
                                _ => $"Action<{@case.Parameters.Select(param => param.TypeName).Join(",")}> {@case.CamelName}"
                            }
                    )
                    .Prepend("Action otherwise")
                    .Join(", "))}})
                {
                    switch (__case)
                    {
                        {{cases.Select(@case =>
                                                            $$"""case {{@case.Index}}: {{@case.CamelName}}({{@case.Parameters.Select(arg => $"(({@case.PascalName}CaseData)__caseData).{arg.PascalName}").Join(", ")}}); break;"""
                          ).Join("\n            ")}}
                        default: ThrowInvalidCaseException<byte>(__case); break;
                    }
                }
            """;

        private static UnionCase[] GetCases(INamedTypeSymbol typeSymbol)
        {
            var unorderedCases = (from indexed in typeSymbol.GetMembers().OfType<IMethodSymbol>().Indexed()
                                  let method = indexed.value
                                  let index = indexed.index
                                  where method.IsPartialDefinition
                                      && !method.ReturnsVoid
                                      && !method.IsGenericMethod
                                  where SymbolEqualityComparer.Default.Equals(method.ReturnType, typeSymbol)
                                  select new UnionCase(index, typeSymbol, method)
                                 ).ToArray();

            var possibleDefaults = unorderedCases.Where(x => x.Parameters.Length == 0);
            if (possibleDefaults.Any() && !possibleDefaults.Skip(1).Any())
                possibleDefaults.First().IsDefault = true;

            var cases = unorderedCases.OrderBy(x => x.IsDefault ? 0 : 1).ToArray();
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
            if (@case.IsDefault)
            {
                return $$"""{{@case.Accessibility}} static partial {{typeName}} {{@case.PascalName}}() => default;""";
            }
            else
            {
                var caseArgs = @case.Parameters.Select(param => $"{param.TypeName} {param.CamelName}").Join(", ");
                var caseData =
                    $$"""new {{@case.PascalName}}CaseData({{@case.Parameters.Select(param => param.CamelName).Join(", ")}})""";

                return $$"""
                        {{@case.Accessibility}} static partial {{typeName}} {{@case.PascalName}}({{@case.Parameters.Select(param => $$"""{{param.TypeName}} {{param.CamelName}}""").Join(", ")}})
                                => new {{typeName}}({{@case.Index}}, {{caseData}} );
                        """;
            }
        }

        private string CaseRecords(UnionCase[] cases)
            => cases.Where(x => !x.IsDefault)
                .Select(@case =>
                    $$"""private record struct {{@case.PascalName}}CaseData({{@case.Parameters.Select(param => $$"""{{param.TypeName}} {{param.PascalName}}""").Join(", ")}});"""
                ).Join("\n    ");

        private string GetTypeClosers(INamedTypeSymbol[] typeNesting) => new string('}', typeNesting.Length);
    }

    internal class UnionCase
    {
        public UnionCase(int index, INamedTypeSymbol type, IMethodSymbol method)
        {
            Index = index;
            Accessibility = method.DeclaredAccessibility.GetSourceText();
            PascalName = method.Name.ToPascalCase();
            CamelName = method.Name.ToCamelCase();

            Parameters = method.Parameters.Select(param => new UnionCaseParameter(type, method, param)).ToArray();
        }

        public int Index { get; }
        public object Accessibility { get; }
        public string PascalName { get; }
        public string CamelName { get; }
        public UnionCaseParameter[] Parameters { get; }
        public bool IsDefault { get; internal set; }
    }

    internal class UnionCaseParameter
    {
        public UnionCaseParameter(INamedTypeSymbol type, IMethodSymbol method, IParameterSymbol param)
        {

            PascalName = param.Name.ToPascalCase();
            CamelName = param.Name.ToCamelCase();
            TypeName = param.Type.GetFullName();
        }

        public string PascalName { get; }
        public string CamelName { get; }
        public string TypeName { get; }
    }
}