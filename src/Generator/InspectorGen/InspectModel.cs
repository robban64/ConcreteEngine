using System.Diagnostics;

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
    
    public string GetLabelLiteral() => Symbols.FormatLiteral(Label, true);
}

internal abstract record InputField(string Name);

internal sealed record NumberInput(
    string Name,
    string NumberType,
    bool IsFloat,
    InputStyle Style,
    float Speed,
    float Min,
    float Max,
    string? Format) : InputField(Name)
{
    public int GetComponents() => (int)char.GetNumericValue(NumberType[^1]);

    public static string BitCast(string t1, string t2, string v) => $"Unsafe.BitCast<{t1}, {t2}>({v})";

    public string ToStyleString() => Style switch
    {
        InputStyle.Input => "InputStyle.Input",
        InputStyle.Slider => "InputStyle.Slider",
        InputStyle.Drag => "InputStyle.Drag",
        _ => throw new UnreachableException()
    };

}

internal sealed record ColorInput(string Name, bool HasAlpha) : InputField(Name);

internal sealed record ComboInput(string Name, string Values, string Names, string? Placeholder, int StartAt)
    : InputField(Name);