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


internal sealed record TargetModel(string Name, string TypeNs, EquatableArray<MemberModel> Members)
{
    public string? DisplayName { get; init; }
    
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

