using System;

namespace Scifa.UnionTypes;

/// <summary>
/// Indicaates that a type should be analyzed by the Scifa.UnionTypes packagein order to generate a union type
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class UnionTypeAttribute : Attribute
{
    /// <summary>
    /// Specifies the name used for the field used for the case when serializing to JSON
    /// </summary>
    public string SerializationCaseFieldName { get; set; } = "$case";
}