using System;

namespace Scifa.UnionTypes;

/// <summary>
/// Indicaates that a type should be analyzed by the Scifa.UnionTypes packagein order to generate a union type
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class UnionTypeAttribute : Attribute
{
}