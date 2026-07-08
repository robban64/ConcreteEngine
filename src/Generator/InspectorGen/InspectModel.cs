using Microsoft.CodeAnalysis;

namespace Generator.InspectorGen;

public enum SpecialInspectKind : byte
{
    Invalid,
    RuntimeId,
    StorageId,
    ObjectName,
}

public enum InspectorTypeKind : byte
{
    Invalid,
    RuntimeId,
    StorageId,
    Number,
    Boolean,
    String,
    Array,
    Map,
    Struct,
    Class,
}

internal record struct MemberModel
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public string? DisplayName {get; init;}
    public MemberTypeInfo TypeInfo { get; init; }
}

internal sealed record TargetModel(string Name, string TargetNamespace, EquatableArray<TargetMemberInfo> Members)
{
    public string? DisplayName { get; init; }
}

internal sealed record TargetMemberInfo()
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required string TypeNamespace { get; init; }
    public required IInspectField? Field { get; init; }
}




internal interface IInspectField : IEquatable<IInspectField>
{
    string Label { get; }
    string ValueType { get; }
}
internal sealed record InputField : IInspectField
{
    public required string Label { get; set; }
    public required string ValueType { get; set; } 
    public string? Format { get; set; }
    public bool Slider { get; set; }
    public bool Drag { get; set; }

    public float Min { get; set; }
    public float Max { get; set; }
    public float Speed { get; set; }

    public bool Equals(IInspectField? other) => other is InputField field && Equals(field);
}

internal sealed record ColorField : IInspectField
{
    public string ValueType => "Float4";
    public required string Label { get; set; }

    public bool HasAlpha { get; set; }

    public bool Equals(IInspectField? other) => other is ColorField field && Equals(field);

}

internal sealed record ComboField: IInspectField
{
    public string ValueType => "Int1";
    public required string Label { get; set; }
    public string? Placeholder { get; set; }
    public int StartAt { get; set; }

    public bool Equals(IInspectField? other) => other is ComboField field && Equals(field);
}