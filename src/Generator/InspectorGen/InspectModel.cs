using System.Collections.Specialized;
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
    EquatableArray<TargetMemberInfo> Members)
{
    public string? DisplayName { get; init; }
}

internal sealed record TargetMemberInfo(string Name, string TargetNs, string TypeName, MemberInfo Info)
{
    public InputField? Input { get; init; }
    public string? Segment { get; init; }
    public string? IncludeName { get; init; }
    public MemberInfo? ParentInfo { get; init; }
}

internal abstract record InputField(string Name, string Label);

internal sealed record NumberInput(
    string Name,
    string Label,
    string ValueType,
    InputStyle InputStyle,
    float Speed,
    float Min,
    float Max,
    string? Format) : InputField(Name, Label)
{
    public bool IsFloat() => ValueType.StartsWith("Float");
    public int GetComponents() => (int)char.GetNumericValue(ValueType[^1]);
}

internal sealed record ColorInput(string Name, string Label, bool HasAlpha) : InputField(Name, Label);
internal sealed record ComboInput(
    string Name,
    string Label,
    string Values,
    string Names,
    string? Placeholder,
    int StartAt) : InputField(Name, Label);