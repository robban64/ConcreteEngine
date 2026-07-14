using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Core.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class FloatInput<T> : InputField where T : unmanaged, IFloatValue
{
    public readonly InputStyle Style;

    public T Value;
    public float Speed, Min, Max;

    public String8Utf8 Format;

    private readonly Func<T> _getter;
    private readonly Action<T> _setter;

    private FrameStepper _stepper = new(4);

    //private readonly delegate*<int, byte*, float*, byte*, float, float, float, bool> _drawer;

    public FloatInput(
        string label,
        InputStyle style,
        Func<T> getter,
        Action<T> setter,
        float speed = 1f,
        float min = 0,
        float max = 0,
        string format = "%.2f") : base(label, InputKind.Float)
    {
        Style = style;
        _getter = getter;
        _setter = setter;
        Format = format;
        Speed = speed;
        Min = min;
        Max = max;

        if (T.Components == 1) LabelPlacement = LabelPlacement.Inline;
    }

    public bool Draw()
    {
        if (_stepper.Tick()) Value = _getter();
        var value = Value;
        var format = Format;
        var label = ApplyLabelLayout();
        var changed = Style switch
        {
            InputStyle.Input => T.DrawInput(label, (float*)&value, (byte*)&format),
            InputStyle.Slider => T.DrawSlider(label, (float*)&value, (byte*)&format, Min, Max),
            InputStyle.Drag => T.DrawDrag(label, (float*)&value, (byte*)&format, Speed, Min, Max),
            _ => false
        };
        if (changed && ShouldTrigger())
        {
            _setter(Value = value);
            return true;
        }

        return false;
    }
}

internal sealed unsafe class IntInput<T> : InputField where T : unmanaged, IIntValue
{
    public readonly InputStyle Style;

    public T Value;
    public int Min, Max;
    public float Speed;

    private readonly Func<T> _getter;
    private readonly Action<T> _setter;

    private FrameStepper _stepper = new(4);

    public IntInput(
        string label,
        InputStyle style,
        Func<T> getter,
        Action<T> setter,
        float speed = 1f,
        int min = 0,
        int max = 0) : base(label, InputKind.Int)
    {
        Style = style;
        _getter = getter;
        _setter = setter;
        Speed = speed;
        Min = min;
        Max = max;

        if (T.Components == 1) LabelPlacement = LabelPlacement.Inline;
    }

    public bool Draw()
    {
        if (_stepper.Tick()) Value = _getter();
        var value = Value;
        var label = ApplyLabelLayout();
        var changed = Style switch
        {
            InputStyle.Input => T.DrawInput(label, (int*)&value),
            InputStyle.Slider => T.DrawSlider(label, (int*)&value, Min, Max),
            InputStyle.Drag => T.DrawDrag(label, (int*)&value, Speed, Min, Max),
            _ => false
        };
        if (changed && ShouldTrigger())
        {
            _setter(Value = value);
            return true;
        }

        return false;
    }
}

internal sealed unsafe class ColorInput : InputField
{
    public bool HasAlpha;

    public Color4 Value;

    private readonly Func<Color4> _getter;
    private readonly Action<Color4> _setter;

    private FrameStepper _stepper = new(4);

    public ColorInput(string label, Func<Color4> getter, Action<Color4> setter, bool hasAlpha = true)
        : base(label, InputKind.Color)
    {
        _getter = getter;
        _setter = setter;
        HasAlpha = hasAlpha;
        LabelPlacement = LabelPlacement.Top;
    }

    public bool Draw()
    {
        if (_stepper.Tick()) Value = _getter();
        var value = Value;
        var label = ApplyLabelLayout();

        ImGui.PushID(DrawId);
        var changed = HasAlpha
            ? ImGui.ColorEdit4(label, &value.R)
            : ImGui.ColorEdit3(label, &value.R);
        ImGui.PopID();

        if (changed && ShouldTrigger())
        {
            _setter(Value = value);
            return true;
        }

        return false;
    }
}