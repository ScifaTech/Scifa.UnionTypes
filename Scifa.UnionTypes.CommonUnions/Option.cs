using UnionTypes;

namespace Scifa.UnionTypes.CommonUnions
{
    [UnionType]
    public partial struct Option<T>
    {
        public static partial Option<T> None();
        public static partial Option<T> Some(T value);

        public static implicit operator Option<T>(UntypedNone _) => Option<T>.None();
    }

    public static class Option
    {
        public static Option<T> Some<T>(T value) => Option<T>.Some(value);
        public static UntypedNone None => new UntypedNone();


        public static Option<U> Map<T, U>(this Option<T> @this, Func<T, U> map) => @this.Match(some: x => Option<U>.Some(map(x)), none: () => Option<U>.None());
        public static Option<U> Bind<T, U>(this Option<T> @this, Func<T, Option<U>> map) => @this.Match(some: x => map(x), none: Option<U>.None);
    }

    public readonly ref struct UntypedNone { }
}