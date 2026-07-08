namespace ConcreteEngine.Editor.Lib.Inspection;

public enum FieldFetchDelay : ushort
{
    None = 0,
    Low = 4,
    Medium = 40,
    High = 160,
    VeryHigh = 1440
}

public enum LabelPlacement : byte
{
    None,
    Top,
    Inline,
}
