using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Scifa.UnionTypes;
using System.Reflection;
using Xunit;

namespace Tests;

public class OptionTests
{
    [Fact]
    public void Equality_None()
        => Option<string>.None().Should().BeEquivalentTo(Option<string>.None());

    [Property]
    public void Equality_Some(NonNull<object> value)
        => typeof(OptionTests)
            .GetMethod(nameof(Equality_Some_T), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(value.Get.GetType())
            .Invoke(this, new object[] { value.Get });

    private void Equality_Some_T<T>(T value)
       => Option.Some(value).Should().BeEquivalentTo(Option<T>.Some(value));
}