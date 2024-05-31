using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
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
        private readonly bool _supportsSerialization;
        private readonly string _caseFieldName = "$case";

        private UnionGeneratorImpl(Compilation compilation, INamedTypeSymbol namedTypeAttributeSymbol)
        {
            _compilation = compilation;
            _namedTypeAttributeSymbol = namedTypeAttributeSymbol;
            _supportsSerialization = _compilation.GetTypeByMetadataName("System.Text.Json.Serialization.JsonConverter`1") is not null;
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
            var cases = GetCases(typeSymbol);
            return SourceText.From($$"""
                {{GetUsings().OrderBy(u => (u.StartsWith("System"), u)).Select(u => $"using {u};").Join("\n")}}

                namespace {{declaredNamespace}};

                {{typeNesting.Take(typeNesting.Length - 1).Select(GetTypeHeader).Join("\n")}}
                [DebuggerDisplay("{DebugView()}")]
                {{SerializationConverterAttribute(typeSymbol)}}
                {{GetTypeHeader(typeNesting.Last())}}
                
                {{GetUnionTypeBody(typeSymbol, cases)}}                
                {{GetTypeClosers(typeNesting)}}
                
                {{SerializationConverter(typeSymbol, cases)}}
                """.NormalizeLineEndings(),
                Encoding.UTF8);
        }

        private string[] GetUsings()
            => [
                "System",
                "System.Runtime.CompilerServices",
                "System.Runtime.InteropServices",
                "System.Diagnostics",
                ..(_supportsSerialization ? new[]{
                    "System.Text.Json",
                    "System.Text.Json.Serialization",
                    "System.Collections.Generic"
                } : [])
            ];

        private string GetUnionTypeBody(INamedTypeSymbol typeSymbol, UnionCase[] cases)
            => $$"""
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

        private string MatchFunctions(UnionCase[] cases)
            => $$"""
                /// <summary>
                /// Given a function to excute for each union case, this method will select the the function for the the current case and execute that function providing the case arguments as function arguments.
                /// </summary>
                {{cases.Select(@case => $$"""/// <param name="{{@case.CamelName}}">The function to execute if this instance is a case of `{{@case.PascalName}}`.</param>""").Join("\n    ")}}
                /// <returns>The result of the given function for the current case.</returns>
                [DebuggerStepThrough]
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
                [DebuggerStepThrough]
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
                [DebuggerStepThrough]
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
                [DebuggerStepThrough]
                public void Do({{(cases.Select(
                        @case =>
                            @case.Parameters switch
                            {
                                { Length: 0 } => $"Action? {@case.CamelName.EscapeIfKeyword()} = null",
                                _ => $"Action<{@case.Parameters.Select(param => param.TypeName).Join(",")}>? {@case.CamelName.EscapeIfKeyword()} = null"
                            }
                    )
                    .Prepend("Action otherwise")
                    .Join(", "))}})
                {
                    switch (__case)
                    {
                        {{cases.Select(@case => $$"""case {{@case.CaseReference}} when {{@case.CamelName.EscapeIfKeyword()}} is not null: {{@case.CamelName.EscapeIfKeyword()}}({{@case.Parameters.Select(param => @case.ValueAccessExpression(param)).Join(", ")}}); break;"""
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
                    $$"""    private record struct {{@case.TypeName}}({{@case.Parameters.Select(param => $$"""{{param.TypeName}} {{param.PascalName}}""").Join(", ")}});"""
                ).Join("\n    ");

        private string GetTypeClosers(INamedTypeSymbol[] typeNesting) => new string('}', typeNesting.Length);

        private string SerializationConverterAttribute(INamedTypeSymbol typeSymbol)
        {
            if (!_supportsSerialization)
                return "";

            return typeSymbol.IsGenericType
                ? $"[JsonConverter(typeof({typeSymbol.Name}JsonConverterFactory))]"
                : $"[JsonConverter(typeof({typeSymbol.Name}JsonConverter))]";
        }

        private string SerializationConverter(INamedTypeSymbol typeSymbol, UnionCase[] cases)
        {
            if (!_supportsSerialization)
                return "";

            var readReturnAnnotation = typeSymbol.IsValueType ? "" : "?";
            var typePrefix = typeSymbol.Unfold(x => x.ContainingType).Reverse().Select(x => x.GetLocalName()).Join(".");
            if (typePrefix.Length > 0)
                typePrefix += ".";

            return typeSymbol.IsGenericType
                ? $$"""
                    file class {{typeSymbol.Name}}JsonConverterFactory : JsonConverterFactory
                    {
                        public override bool CanConvert(Type typeToConvert)
                            => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof({{typePrefix + typeSymbol.GetLocalName(leaveGenericOpen: true)}});

                        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
                            => (JsonConverter?)Activator.CreateInstance(typeof({{typeSymbol.Name}}JsonConverter{{typeSymbol.GetTypeParameterSpec(leaveGenericOpen: true)}}).MakeGenericType(typeToConvert.GetGenericArguments()));

                        {{GetConverter("private")}}
                    }
                    """
                : GetConverter("file");

            string GetConverter(string access)
                => $$"""
                    {{access}} class {{typeSymbol.Name}}JsonConverter{{typeSymbol.GetTypeParameterSpec()}} : JsonConverter<{{typePrefix + typeSymbol.GetLocalName()}}>
                    {
                        private const string CaseFieldName = "{{_caseFieldName}}";
                
                        public override {{typePrefix + typeSymbol.GetLocalName()}}{{readReturnAnnotation}} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                        {
                            var fields = JsonSerializer.Deserialize<Dictionary<string, global::System.Text.Json.JsonElement>>(ref reader, options);
                
                            if (fields is null)
                                return default;
                
                            if (!fields.TryGetValue(CaseFieldName, out var caseToken))
                            {
                                throw new InvalidOperationException($"Unable to deserialize instance of {{typePrefix + typeSymbol.GetLocalName()}} as no case was specified.");
                            }
                
                            var caseString = caseToken.GetString();
                            if (!Enum.TryParse<{{typePrefix + typeSymbol.GetLocalName()}}.CaseName>(caseString, out var caseValue))
                            {
                                throw new InvalidOperationException($"Unable to deserialize instance of {{typePrefix + typeSymbol.GetLocalName()}} as case '{caseString}' is unrecognised.");
                            }
                
                            return caseValue switch
                            {
                    {{cases.Select(@case => $$"""
                                {{typePrefix + typeSymbol.GetLocalName()}}.CaseName.{{@case.PascalName}} => 
                                    {{typePrefix + typeSymbol.GetLocalName()}}.{{@case.PascalName}}(
                    {{@case.Parameters.Select(param => $"""
                                        {param.CamelName.EscapeIfKeyword()}: GetValue<{param.TypeName}>("{param.CamelName}", fields, options)!
                    """).Join(",\n")}}
                                    ),
                    """).Join("\n")}}
                                _ => throw new InvalidOperationException($"Unable to deserialize instance of {{typePrefix + typeSymbol.GetLocalName()}} as case '{caseString}' is unrecognised."),
                            };
                        }
                
                        private static TValue GetValue<TValue>(in string propertyName, in Dictionary<string, global::System.Text.Json.JsonElement> fields, in JsonSerializerOptions options)
                        {
                            if (!fields.TryGetValue(propertyName, out var valueToken))
                            {
                                throw new InvalidOperationException($"Unable to deserialize instance of {{typePrefix + typeSymbol.GetLocalName()}} as no value for '{propertyName}' was specified.");
                            }
                
                            return JsonSerializer.Deserialize<TValue>(valueToken.GetRawText(), options)!;
                        }
                
                        public override void Write(Utf8JsonWriter writer, {{typePrefix + typeSymbol.GetLocalName()}} value, JsonSerializerOptions options)
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName(CaseFieldName);
                            writer.WriteStringValue(value.Case.ToString());
                                
                            value.Do(
                    {{cases.Select(@case => $$""" 
                                {{@case.CamelName.EscapeIfKeyword()}}: ({{@case.Parameters.Select(x => x.CamelName).Join(", ")}}) =>
                                {
                    {{@case.Parameters.Select(param => $$"""
                                    Write("{{param.CamelName}}", {{param.CamelName.EscapeIfKeyword()}});
                    """).Join("\n")}}
                                }
                    """).Join(",\n")}}
                            );
                
                            writer.WriteEndObject();
                                
                            void Write<TValue>(string propertyName, TValue value)
                            {
                                writer.WritePropertyName(propertyName);
                                JsonSerializer.Serialize(writer, value, options);
                            }
                        }
                    }
                    """;
        }

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

        public string ValueAccessExpression(UnionCaseParameter param, string objectExpression = "")
            => this.EmitCaseType ? $"(({this.TypeName}){objectExpression}__caseData).{param.PascalName}" : $"(({this.TypeName}){objectExpression}__caseData)";
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
