using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
namespace Scifa.UnionTypes;

[UnionType]
public readonly partial struct Option<T>
{
    public static partial Option<T> None();
    public static partial Option<T> Some(T value);

    /// <summary>
    /// Get the value if specified, otherwise return a default value
    /// </summary>
    public T DefaultValue(T defaultValue) => Match(some: x => x, none: () => defaultValue);

    /// <summary>
    /// Get the value if specified, otherwise return a default value
    /// </summary>
    public T DefaultValue(Func<T> defaultValueFactory) => Match(some: x => x, none: defaultValueFactory);


    /// <summary>
    /// Implicitly convert an <see cref="UntypedNone"/> to an <see cref="Option{T}"/>.
    /// </summary>
    public static implicit operator Option<T>(UntypedNone _) => Option<T>.None();

    /// <summary>
    /// Implicitly convert a value to <c>Option.Some(value)</c>. If <see langword="null" /> is provided, the result will be <c>Option.Some(null)</c>.
    /// </summary>
    public static implicit operator Option<T>(T value) => Option<T>.Some(value);
}

public static class Option
{
    public static Option<T> Some<T>(T value) => Option<T>.Some(value);
    public static UntypedNone None => new UntypedNone();

    public static Option<T> FromNullable<T>(T? value) where T : struct => value.HasValue ? Some(value.Value) : None;
    public static Option<T> FromNullable<T>(T? value) where T : class => value is T ? Some(value) : None;

    public static Option<U> Map<T, U>(this Option<T> @this, Func<T, U> map) => @this.Bind(x => Option<U>.Some(map(x)));
    public static Option<U> Bind<T, U>(this Option<T> @this, Func<T, Option<U>> map) => @this.Match(some: x => map(x), none: Option<U>.None);

    public static T? ToNullable<T>(this Option<T> @this) where T : notnull => @this.Match(none: () => default(T?), some: x => x);

    public static bool TryGetValue<T>(this Option<T> option, [NotNullWhen(true)] out T? value) where T : class
    {
        (var result, value) = option.Match(
            some: v => (true, (T?)v),
            none: () => (false, null)
        );
        return result;
    }
    public static bool TryGetValue<T>(this Option<T> option, [NotNullWhen(true)] out T? value) where T : struct
    {
        (var result, value) = option.Match(
            some: v => (true, (T?)v),
            none: () => (false, null)
        );
        return result;
    }

    public static Option<T> TryGetAt<T>(this IReadOnlyList<T> source, int index)
        => source.Count > index ? Some(source[index]) : Option<T>.None();

    public static IEnumerable<T> ToEnumerable<T>(this Option<T> option)
        => option.Match(
            some: v => [v],
            none: Enumerable.Empty<T>
        );

    /// <summary>
    /// Gets all non-None values from the sequence.
    /// </summary>
    /// <typeparam name="T">The type of values in the given sequence</typeparam>
    /// <param name="source">A sequnce of optional values to filter</param>
    public static IEnumerable<T> Choose<T>(this IEnumerable<Option<T>> source)
    {
        foreach (var option in source)
        {
            var (isSome, value) = option.Match(
                some: v => (true, v),
                none: () => (false, default)
            );

            if (isSome)
                yield return value;
        }
    }
}

public readonly ref struct UntypedNone { }