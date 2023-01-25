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