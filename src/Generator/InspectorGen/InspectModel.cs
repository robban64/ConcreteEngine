namespace Generator.InspectorGen;

internal enum FieldKind : byte
{
    Input,
    Slider,
    Drag,
    Combo,
}


internal sealed record InspectModel()
{
    public required string TypeName { get; set; }
    public required string MemberName { get; set; }
    public required string MemberTypeName { get; set; }

    public required string DisplayName { get; set; }
    public required IInspectField Field { get; set; }
}


internal interface IInspectField
{
    string ValueType { get; }
}
internal sealed record InputField() : IInspectField
{
    public required string ValueType { get; set; } 
    public string? Format { get; set; }
    public FieldKind Kind { get; set; }
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
