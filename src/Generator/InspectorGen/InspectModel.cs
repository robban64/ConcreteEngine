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


internal sealed record TargetModel(string Name, string TargetNamespace, EquatableArray<TargetMemberInfo> Members)
{
    public string? DisplayName { get; init; }
}

internal sealed record TargetMemberInfo()
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required string TypeNamespace { get; init; }
    public required IInspectField Field { get; init; }
}


internal record struct MemberTypeInfo(
    bool IsValueType,
    bool IsUnmanaged,
    bool IsReadOnly,
    TypeKind TypeKind,
    SpecialType SpecialType);

internal record struct MemberModel
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public string? DisplayName {get; init;}
    public MemberTypeInfo TypeInfo { get; init; }
    //public string? DisplayName { get; init; }
    //public required Type MemberType;
    //public required string IsCollection;
    //public required string FormatHint;
    //public required string CollectionStyle;
}


internal interface IInspectField
{
    string ValueType { get; }
}
internal sealed record InputField() : IInspectField
{
    public required string ValueType { get; set; } 
    public string? Format { get; set; }
    public bool Slider { get; set; }
    public bool Drag { get; set; }

    public float Min { get; set; }
    public float Max { get; set; }
    public float Speed { get; set; }
}

internal sealed record ColorField() : IInspectField
{
    public string ValueType => "Float4";
    public bool HasAlpha { get; set; }
}

internal sealed record ComboField(): IInspectField
{
    public string ValueType => "Int1";
    public string? Placeholder { get; set; }
    public int StartAt { get; set; }
}