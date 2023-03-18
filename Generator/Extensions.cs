using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static IEnumerable<(T value, int index)> Indexed<T>(this IEnumerable<T> source)
            => source.Select((value, i) => (value, i));

        public static string Join(this IEnumerable<string> source, string separator)
            => string.Join(separator, source);
    }
}