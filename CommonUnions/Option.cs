using System;
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
    /// Implicitly convert a value to an Option
    /// </summary>
    public static implicit operator Option<T>(T value) => Option<T>.Some(value);
}

public static class Option
{
    public static Option<T> Some<T>(T value) => Option<T>.Some(value);
    public static UntypedNone None => new UntypedNone();


    public static Option<U> Map<T, U>(this Option<T> @this, Func<T, U> map) => @this.Bind(x => Option<U>.Some(map(x)));
    public static Option<U> Bind<T, U>(this Option<T> @this, Func<T, Option<U>> map) => @this.Match(some: x => map(x), none: Option<U>.None);
}

public readonly ref struct UntypedNone { }