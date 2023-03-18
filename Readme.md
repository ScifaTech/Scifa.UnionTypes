# Union Types
Allowing C# to represent unions. Also known as "sum types" or "intersection types", union types allow modelling the concept of a thing be wither A _OR_ B.

Examples:
- Modelling PATCH operations: a value could either be set or not set (Option/Maybe type)
- A call to a database will result in either a dataset or an error (Result type)
- Money coming into your account could be income or a refund from a shop or someone paying you back for something

# How to use
1. Install the package
2. create a type and annotate with `[UnionType]`
3. Add static methods returning the containing type

## Examples

### Option<T> = None | Some(T)
```csharp
using UnionTypes; // 1. Install the package

[UnionType] // 2. create a type and annotate with `[UnionType]`
public readonly partial record struct Option<T>
{
    // 3. Add static methods returning the containing type
    public static partial Option<T> None();
    public static partial Option<T> Some(T value);


    // Map and Bind are common methods on option types
    public Option<TOut> Map<TOut>(Func<T, TOut> mapper)
        => Match(
            none: () => Option<TOut>.None(),
            some: x => Option<TOut>.Some(mapper(x))
        );
    public Option<TOut> Bind<TOut>(Func<T, Option<TOut>> mapper)
        => Match(
            none: () => Option<TOut>.None(),
            some: x => mapper(x)
        );
}
```

### Singly-linked list LList<T> = Empty | Node<T>

```csharp
using UnionTypes;

[UnionType]
public readonly partial record struct LList<T>
{
    public static partial Option<T> Empty();
    public static partial Option<T> Node(T value, LList<T> next);
}
```

```csharp
var list = LList.Node(1, LList.Node(2, LList.Node(3)));

```

# Isn't this the same thing as OneOf?

Yeah, pretty much. I wrote this becuase I prefer the specific types than generic catch-all types and I wanted something which would work nicely with structs too.
As a bonus, it appears to be a little bit faster (though with slightly larger code size) but that wasn't the primary goal.

```md
// * Summary *

BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22000.1574/21H2/SunValley)
Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


|                                Method |      Mean |     Error |    StdDev | Ratio | Code Size | Allocated | Alloc Ratio |
|-------------------------------------- |----------:|----------:|----------:|------:|----------:|----------:|------------:|
|             CreateAndMatch_None_OneOf | 10.546 ns | 0.0729 ns | 0.0646 ns |  1.00 |     333 B |         - |          NA |
|        CreateAndMatch_None_UnionTypes |  6.391 ns | 0.0770 ns | 0.0643 ns |  0.61 |     389 B |         - |          NA |
| CreateAndMatch_None_UnionTypes_static |  6.289 ns | 0.0464 ns | 0.0411 ns |  0.60 |     389 B |         - |          NA |
|                                       |           |           |           |       |           |           |             |
|             CreateAndMatch_Some_OneOf | 10.868 ns | 0.0976 ns | 0.0815 ns |  1.00 |     336 B |         - |          NA |
|        CreateAndMatch_Some_UnionTypes |  6.496 ns | 0.0319 ns | 0.0298 ns |  0.60 |     404 B |         - |          NA |
| CreateAndMatch_Some_UnionTypes_static |  6.493 ns | 0.0506 ns | 0.0449 ns |  0.60 |     404 B |         - |          NA |
```