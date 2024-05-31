using FluentAssertions;
using Scifa.UnionTypes;
using System.Text.Json;
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

    public static IEnumerable<object[]> JsonElementExamples()
    {
        yield return new object[] { JsonElement.Null(), """null""" };
        yield return new object[] { JsonElement.Boolean(true), """{"$case":"Boolean","value":true}""" };
        yield return new object[] { JsonElement.Boolean(false), """{"$case":"Boolean","value":false}""" };
        yield return new object[] { JsonElement.Number(42), """{"$case":"Number","value":42}""" };
        yield return new object[] { JsonElement.String("Hello"), """{"$case":"String","value":"Hello"}""" };
        yield return new object[] { JsonElement.Object([new KeyValuePair<string, JsonElement>("key", JsonElement.String("value"))]), """{"$case":"Object","properties":[{"Key":"key","Value":{"$case":"String","value":"value"}}]}""" };
        yield return new object[] { JsonElement.List([JsonElement.String("value")]), """{"$case":"List","elements":[{"$case":"String","value":"value"}]}""" };
        yield return new object[] { JsonElement.List([
            JsonElement.Object([new KeyValuePair<string, JsonElement>("key", JsonElement.String("value"))])
        ]), """{"$case":"List","elements":[{"$case":"Object","properties":[{"Key":"key","Value":{"$case":"String","value":"value"}}]}]}""" };
    }

    [Theory]
    [MemberData(nameof(JsonElementExamples))]
    public void Can_serialize(JsonElement value, string serialized)
    {
        JsonSerializer.Serialize(value).Should().Be(serialized);
    }

    [Theory]
    [MemberData(nameof(JsonElementExamples))]
    public void Can_deserialize(JsonElement value, string serialized)
    {
        JsonSerializer.Deserialize<JsonElement>(serialized).Should().BeEquivalentTo(value);
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