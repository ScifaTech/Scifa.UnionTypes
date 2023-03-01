using UnionTypes;

namespace Scifa.UnionTypes.CommonUnions
{
    public static class Option
    {
        public static Option<T> Some<T>(T value) => Option<T>.Some(value);
        public static UntypedNone None => new UntypedNone();
    }

    public readonly ref struct UntypedNone { }

    [UnionType]
    public partial struct Option<T>
    {
        public static partial Option<T> None();
        public static partial Option<T> Some(T value);

        public Option<U> Map<U>(Func<T, U> map) => Match(some: x => Option<U>.Some(map(x)), none: () => Option<U>.None());
        public Option<U> Bind<U>(Func<T, Option<U>> map) => Match(some: x => map(x), none: Option<U>.None);

        public static implicit operator Option<T>(UntypedNone _) => Option<T>.None();
    }
}