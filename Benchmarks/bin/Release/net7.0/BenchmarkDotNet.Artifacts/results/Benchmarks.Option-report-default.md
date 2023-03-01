
BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22000.1574/21H2/SunValley)
Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


                    Method |      Mean |     Error |    StdDev | Ratio | Code Size | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|------:|----------:|----------:|------------:|
      CreateAndMatch_OneOf | 11.093 ns | 0.0606 ns | 0.0506 ns |  1.00 |     336 B |         - |          NA |
 CreateAndMatch_UnionTypes |  6.412 ns | 0.0352 ns | 0.0312 ns |  0.58 |     404 B |         - |          NA |
