namespace ConcreteEngine.Editor.Lib.Inspection;

public enum FieldGetDelay : ushort
{
    None = 0,
    Low = 4,
    Medium = 40,
    High = 160,
    VeryHigh = 1440
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