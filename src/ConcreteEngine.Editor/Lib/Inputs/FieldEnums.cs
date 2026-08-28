using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Inputs;

public enum LabelPlacement : byte
{
    None, Top, Inline,
}

public enum InputKind : byte
{
    Int, Float, Color, Bool, Combo, Text, Group, Texture,
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

    public override bool Draw()
    {
        var value = Value;

        var strId = _stringId;
        var changed = ImGui.Checkbox((byte*)&strId, &value);
        if (changed)
        {
            _setter(value);
            Value = value;
        }

        return changed;
    }
}