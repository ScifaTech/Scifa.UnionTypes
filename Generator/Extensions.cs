using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace UnionTypes.Generator
{
    internal static class Extensions
    {
        public static void Deconstruct<TKey, TValue>(in this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
            => (key, value) = (kvp.Key, kvp.Value);
        internal static string GetSourceText(this Accessibility source)
            => source switch
            {
                Accessibility.NotApplicable => string.Empty,
                Accessibility.Private => "private",
                Accessibility.ProtectedAndInternal => "private protected",
                Accessibility.Protected => "protected",
                Accessibility.Internal => "internal",
                Accessibility.ProtectedOrInternal => "protected internal",
                Accessibility.Public => "public",
            };

        public static string GetFullName(this INamespaceOrTypeSymbol symbol)
                => symbol switch
                {
                    ITypeParameterSymbol => GetLocalName(symbol),
                    INamespaceOrTypeSymbol type => symbol
                                        .Unfold(x => (INamespaceOrTypeSymbol)x.ContainingType ?? x.ContainingNamespace, ns => ns.Name)
                                        .Where(x => !string.IsNullOrWhiteSpace(x))
                                        .Reverse()
                                        .Append(GetLocalName(type))
                                        .Join("."),
                    _ => GetLocalName(symbol)
                };


        public static string GetLocalName(this INamespaceOrTypeSymbol symbol)
            => symbol switch
            {
                INamedTypeSymbol type => symbol.Name + GetTypeParameterSpec(type),
                IArrayTypeSymbol type => GetLocalName(type.ElementType) + "[]",
                INamespaceSymbol ns => ns.Name,
                ITypeParameterSymbol sym => sym.Name
            };
        private static string GetTypeParameterSpec(INamedTypeSymbol type)
            => type switch
            {
                { IsGenericType: false } => string.Empty,
                { IsUnboundGenericType: true } => $"<{type.TypeParameters.Select(p => p.Name).Join(",")}>",
                _ => $"<{type.TypeArguments.Select(p => p.GetFullName()).Join(",")}>",
            };

        public static IEnumerable<T> Unfold<T>(this T symbol, Func<T, T?> unfold)
        {
            var elem = unfold(symbol);
            if (elem is not null)
            {
                do
                {
                    yield return elem;
                    elem = unfold(elem);
                } while (elem is not null);
            }
        }
        public static IEnumerable<TOut> Unfold<TIn, TOut>(this TIn symbol, Func<TIn, TIn?> unfold, Func<TIn, TOut> selector)
        {
            var elem = unfold(symbol);
            if (elem is not null)
            {
                do
                {
                    yield return selector(elem);
                    elem = unfold(elem);
                } while (elem is not null);
            }
        }
        public static string ToCamelCase(this string source)
            => char.IsLower(source[0])
            ? source
            : char.ToLower(source[0]) + source.Substring(1);

        public static string ToPascalCase(this string source)
            => char.IsUpper(source[0])
            ? source
            : char.ToUpper(source[0]) + source.Substring(1);

        public static string EscapeIfKeyword(this string v) => v switch
        {
            "abstract" or "as" or "base" or "bool" or "break" or "byte" or "case" or "catch" or "char" or "checked" or "class"
            or "const" or "continue" or "decimal" or "default" or "delegate" or "do" or "double" or "else" or "enum" or ""
            or "event" or "explicit" or "extern" or "false" or "finally" or "fixed" or "float" or "for" or "foreach" or "goto"
            or "if" or "implicit" or "in" or "int" or "interface" or "internal" or "is" or "lock" or "long" or "" or "namespace"
            or "new" or "null" or "object" or "operator" or "out" or "override" or "params" or "private" or "protected"
            or "public" or "readonly" or "ref" or "return" or "sbyte" or "sealed" or "short" or "sizeof" or "stackalloc" or ""
            or "static" or "string" or "struct" or "switch" or "this" or "throw" or "true" or "try" or "typeof" or "uint"
            or "ulong" or "unchecked" or "unsafe" or "ushort" or "using" or "virtual" or "void" or "volatile" or "while" or "add"
            or "and" or "alias" or "ascending" or "args" or "async" or "await" or "by" or "descending" or "dynamic" or "equals"
            or "file" or "from" or "get" or "global" or "group" or "init" or "into" or "join" or "let" or "managed" or "nameof"
            or "nint" or "not" or "notnull" or "nuint" or "on" or "or" or "orderby" or "partial" or "partial" or "record"
            or "remove" or "required" or "scoped" or "select" or "set" or "unmanaged " or "unmanaged" or "value" or "var"
            or "when" or "where" or "where" or "with" or "yield"
            => "@" + v,
            _ => v
        };


        public static IEnumerable<(T value, int index)> Indexed<T>(this IEnumerable<T> source)
            => source.Select((value, i) => (value, i));

        public static string Join(this IEnumerable<string> source, string separator)
            => string.Join(separator, source);

        public static string NormalizeLineEndings(this string source)
            => source.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
    }
}