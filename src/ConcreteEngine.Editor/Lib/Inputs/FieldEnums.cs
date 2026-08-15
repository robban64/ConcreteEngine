using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

public enum LabelPlacement : byte
{
    None, Top, Inline,
}

public enum InputKind : byte
{
    Int, Float, Color, Bool, Combo, Text, Group
}

public enum InputTrigger : byte
{
    OnChange, AfterChange, AfterChangeDeActive
}

internal sealed unsafe class CheckboxInput : InputField
{
    public bool Value;
    private readonly Action<bool> _setter;

    public CheckboxInput(string label, Action<bool> setter) : base(label, InputKind.Bool)
    {
        _setter = setter;
    }

    public bool Draw()
    {
        var value = Value;

        DrawLabel();
        var changed = ImGui.Checkbox(StringId, &value);
        if (changed)
        {
            _setter(value);
            Value = value;
        }

        return changed;
    }
}