using FluentAssertions;
using Scifa.UnionTypes;
using Xunit;

namespace Tests
{
    public partial class NullableArgsTests
    {
        [Fact]
        public void Can_create_case_with_nullable_args()
        {
            var x = Union.Case1(0, null, "abc", null);
            x.Should().NotBeNull();
        }

        [UnionType]
        public partial class Union
        {
            public static partial Union Case1(int i1, int? i2, string s, string? s2);
        }
    }
}
