using BenchmarkDotNet.Running;
using System.Reflection;

namespace Benchmarks;

public class Program
{
    public static void Main(string[] args)
        => BenchmarkSwitcher
            .FromAssembly(Assembly.GetExecutingAssembly())
            .Run(args);
}
