using ConcreteEngine.Core.Diagnostics.Time;
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
    private bool _value;
    
    private readonly Func<bool> _getter;
    private readonly Action<bool> _setter;
    
    private FrameStepper _stepper = new(12);

    public CheckboxInput(string label, Func<bool> getter, Action<bool> setter) : base(label, InputKind.Bool)
    {
        _getter = getter;
        _setter = setter;
    }

    public bool Draw()
    {
        if(_stepper.Tick()) _value = _getter();
        var value = _value;
        
        DrawLabel();
        var changed = ImGui.Checkbox(StringId, &value);
        if (changed)
        {
            _setter(value);
            _value = value;
        }
        return changed;
    }
}