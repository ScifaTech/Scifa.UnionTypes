namespace UnionTypes.Playground;

[UnionType]
public readonly partial record struct Thing
{
    public static partial Thing None();

    public static partial Thing FromEnumerable(IEnumerable<Thing> value);

    private static partial Thing PrivateCtor(string value);
}
