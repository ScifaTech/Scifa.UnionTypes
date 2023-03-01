``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22000.1574/21H2/SunValley)
Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


```
|                         Method |      Mean |     Error |    StdDev | Ratio | Code Size | Allocated | Alloc Ratio |
|------------------------------- |----------:|----------:|----------:|------:|----------:|----------:|------------:|
|      CreateAndMatch_None_OneOf | 10.650 ns | 0.0854 ns | 0.0799 ns |  1.00 |     333 B |         - |          NA |
| CreateAndMatch_None_UnionTypes |  6.359 ns | 0.0625 ns | 0.0554 ns |  0.60 |     389 B |         - |          NA |
|                                |           |           |           |       |           |           |             |
|      CreateAndMatch_Some_OneOf | 10.843 ns | 0.0742 ns | 0.0694 ns |  1.00 |     336 B |         - |          NA |
| CreateAndMatch_Some_UnionTypes |  6.531 ns | 0.0531 ns | 0.0415 ns |  0.60 |     404 B |         - |          NA |
