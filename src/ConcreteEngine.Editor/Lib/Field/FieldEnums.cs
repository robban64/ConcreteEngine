namespace ConcreteEngine.Editor.Lib.Field;

public enum LabelPlacement : byte
{
    None, Top, Inline,
}

public enum InputKind : byte
{
    Int, Float, Color, Combo, Text, Group
}

public enum InputTrigger : byte
{
    OnChange, AfterChange, AfterChangeDeActive
}