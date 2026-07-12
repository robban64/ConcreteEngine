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
    EquatableArray<InspectorMember> Members,
    EquatableArray<InspectorGroup> Groups)
{
    public string? DisplayName { get; init; }
}

internal sealed record InspectorGroup(
    string Name,
    string AccessPath,
    MemberInfo Info,
    EquatableArray<InspectorMember> Members);

internal sealed record InspectorMember(string Name, string TargetNs, string TypeName, MemberInfo Info)
{
    public InputField? Input { get; init; }
}

internal abstract record InputField(string Name, string Label); // Label = Name || DisplayName

internal sealed record NumberInput(
    string Name,
    string Label,
    string NumberType,
    InputStyle InputStyle,
    float Speed,
    float Min,
    float Max,
    string? Format) : InputField(Name, Label)
{
    public bool IsFloat() => NumberType.StartsWith("Float");
    public int GetComponents() => (int)char.GetNumericValue(NumberType[^1]);
}

internal sealed record ColorInput(string Name, string Label, bool HasAlpha) : InputField(Name, Label);
internal sealed record ComboInput(
    string Name,
    string Label,
    string Values,
    string Names,
    string? Placeholder,
    int StartAt) : InputField(Name, Label);