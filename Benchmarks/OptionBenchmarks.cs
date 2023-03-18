using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using OneOf;
using OneOf.Types;
using Scifa.UnionTypes.CommonUnions;

namespace Benchmarks;

[MemoryDiagnoser]
[DisassemblyDiagnoser()]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class OptionBenchmarks
{
    [Benchmark(Baseline = true), BenchmarkCategory("Some")]
    public string CreateAndMatch_Some_OneOf() => ((OneOf<string, None>)"abc").Match(x => x, x => string.Empty);
    [Benchmark, BenchmarkCategory("Some")]
    public string CreateAndMatch_Some_UnionTypes() => Option<string>.Some("abc").Match(some: x => x, none: () => string.Empty);
    [Benchmark, BenchmarkCategory("Some")]
    public string CreateAndMatch_Some_UnionTypes_static() => Option.Some("abc").Match(some: x => x, none: () => string.Empty);

    [Benchmark(Baseline = true), BenchmarkCategory("None")]
    public string CreateAndMatch_None_OneOf() => ((OneOf<string, None>)new None()).Match(x => x, x => string.Empty);
    [Benchmark, BenchmarkCategory("None")]
    public string CreateAndMatch_None_UnionTypes() => Option<string>.None().Match(some: x => x, none: () => string.Empty);
    [Benchmark, BenchmarkCategory("None")]
    public string CreateAndMatch_None_UnionTypes_static() => ((Option<string>)Option.None).Match(some: x => x, none: () => string.Empty);
}