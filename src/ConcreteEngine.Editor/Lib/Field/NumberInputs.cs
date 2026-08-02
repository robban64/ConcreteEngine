using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class FloatInput<T> : InputField where T : unmanaged, IInputNumeric<T>
{
    public readonly InputStyle Style;

    public T* Value;
    public float Speed, Min, Max;

    private readonly String8Utf8 _format;

    private readonly Func<T> _getter;
    private readonly Action<T> _setter;

    private FrameStepper _stepper = new(4);

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
        _format = format;
        Speed = speed;
        Min = min;
        Max = max;

        Value = (T*)StringArena.Instance.AllocBytes(Unsafe.SizeOf<T>()).Ptr;
        if (T.Components == 1) LabelPlacement = LabelPlacement.Inline;
    }

    public bool Draw()
    {
        if (_stepper.Tick()) *Value = _getter();

        DrawLabel();
        var changed = Style switch
        {
            InputStyle.Input => T.DrawFloatInput(StringId, Value, _format),
            InputStyle.Slider => T.DrawFloatSlider(StringId, Value, _format, Min, Max),
            InputStyle.Drag => T.DrawFloatDrag(StringId, Value, _format, Speed, Min, Max),
            _ => false
        };
        if (changed && ShouldTrigger())
        {
            _setter(*Value);
            return true;
        }

        return false;
    }
}

internal sealed unsafe class IntInput<T> : InputField where T : unmanaged, IInputNumeric<T>
{
    public readonly InputStyle Style;

    public T* Value;
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

        Value = (T*)StringArena.Instance.AllocBytes(Unsafe.SizeOf<T>()).Ptr;
        if (T.Components == 1) LabelPlacement = LabelPlacement.Inline;
    }

    public bool Draw()
    {
        if (_stepper.Tick()) *Value = _getter();

        DrawLabel();
        var changed = Style switch
        {
            InputStyle.Input => T.DrawIntInput(StringId, Value),
            InputStyle.Slider => T.DrawIntSlider(StringId, Value, Min, Max),
            InputStyle.Drag => T.DrawIntDrag(StringId, Value, Speed, Min, Max),
            _ => false
        };

        if (changed && ShouldTrigger())
        {
            _setter(*Value);
            return true;
        }

        return false;
    }
}

internal sealed unsafe class ColorInput : InputField
{
    public bool HasAlpha;

    public Color4* Value;

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
        Value = (Color4*)StringArena.Instance.AllocBytes(Unsafe.SizeOf<Color4>()).Ptr;
    }

    public bool Draw()
    {
        if (_stepper.Tick()) *Value = _getter();

        DrawLabel();
        var changed = HasAlpha
            ? ImGui.ColorEdit4(StringId, &Value->R)
            : ImGui.ColorEdit3(StringId, &Value->R);

        if (changed && ShouldTrigger())
        {
            _setter(*Value);
            return true;
        }

        return false;
    }
}