using System.Diagnostics;
using Generator.Misc;

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

internal enum SupportedTypes : byte
{
    Int, Float, Bool, DateTime,
}

internal enum InputKind : byte
{
    Int, Float, Color, Combo, Text
}

internal enum InputStyle : byte
{
    Input, Slider, Drag,
}

internal sealed record InspectModel(
    string InspectorName,
    string InspectorNs,
    string TargetName,
    string TargetNs,
    EquatableArray<InspectorGroup> Groups)
{
    public string? DisplayName { get; init; }
}

internal sealed record InspectorGroup(
    bool IsRoot,
    bool IsInputGroup,
    string Name,
    string AccessPath,
    MemberInfo Info,
    EquatableArray<InspectorMember> Members);

internal sealed record InspectorMember(
    string Name,
    string Label,
    string TargetNs,
    string TypeName,
    MemberInfo Info)
{
    public string? Segment { get; init; }
    public InputField? Input { get; init; }

}
