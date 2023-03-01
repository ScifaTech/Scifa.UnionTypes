using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using OneOf;
using OneOf.Types;
using Scifa.UnionTypes.CommonUnions;
using System.Reflection;

namespace Benchmarks;

public class Program
{
    public static void Main(string[] args)
        => BenchmarkSwitcher
            .FromAssembly(Assembly.GetExecutingAssembly())
            .Run(args);
}

[MemoryDiagnoser]
[DisassemblyDiagnoser]
public class Option
{
    [Benchmark(Baseline = true)]
    public string CreateAndMatch_OneOf() => ((OneOf<string, None>)"abc").Match(x => x, x => string.Empty);

    [Benchmark]
    public string CreateAndMatch_UnionTypes() => Option<string>.Some("abc").Match(some: x => x, none: () => string.Empty);
}