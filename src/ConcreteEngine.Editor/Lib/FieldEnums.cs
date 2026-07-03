namespace ConcreteEngine.Editor.Lib;

public enum FieldGetDelay : ushort
{
    None = 0,
    Low = 4,
    Medium = 40,
    High = 160,
    VeryHigh = 1440
}

public enum FieldKind : byte
{
    Input,
    Slider,
    Drag,
    Combo,
    Composite,
    InputText
}

public enum FieldLabelPlacement : byte
{
    None,
    Top,
    Inline,
}

public enum FieldTrigger : byte
{
    OnChange,
    AfterChange,
    AfterChangeDeactive
}