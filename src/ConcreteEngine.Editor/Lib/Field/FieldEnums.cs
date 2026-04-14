namespace ConcreteEngine.Editor.Lib.Field;

public enum FieldGetDelay : ushort
{
    None = 0,
    Low = 4,
    Medium = 40,
    High = 160,
    VeryHigh = 1440
}

public enum FieldWidgetKind : byte
{
    Input,
    Slider,
    Drag,
    Combo
}

public enum FieldLayout : byte
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