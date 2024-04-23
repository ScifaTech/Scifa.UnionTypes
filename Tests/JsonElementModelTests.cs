using Scifa.UnionTypes;
using Xunit;

namespace Tests;

public class JsonElementModelTests
{
    [Fact]
    public void Can_access_case_name()
    {
        var element = JsonElement.String("Hello");
        Assert.Equal(JsonElement.CaseName.String, element.Case);
    }
}

[UnionType]
public partial class JsonElement
{
    public static partial JsonElement Null();
    public static partial JsonElement Boolean(bool value);
    public static partial JsonElement Number(decimal value);
    public static partial JsonElement String(string value);
    public static partial JsonElement Object(KeyValuePair<string, JsonElement>[] properties);
    public static partial JsonElement List(JsonElement[] elements);

}