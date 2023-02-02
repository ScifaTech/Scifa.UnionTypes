using UnionTypes;

namespace Playground;

[UnionType]
public readonly partial record struct Option<T>
{
    public static readonly Option<T> Null = None();

    public static partial Option<T> None();

    public static partial Option<T> Some(T value);

    public Option<TOut> Map<TOut>(Func<T, TOut> mapper)
        => Match(
            none: () => Option<TOut>.None(),
            some: x => Option<TOut>.Some(mapper(x))
        );

    public Option<TOut> Bind<TOut>(Func<T, Option<TOut>> mapper)
        => Match(
            none: () => Option<TOut>.None(),
            some: x => mapper(x)
        );
}

public class OptionExamples
{
    public static void Main(string[] args)
    {
        var thing = Option<string>.Some("myThing");
        Console.WriteLine(thing.Match(() => "nothing here", x => "ooh, we got: " + x));
    }
}