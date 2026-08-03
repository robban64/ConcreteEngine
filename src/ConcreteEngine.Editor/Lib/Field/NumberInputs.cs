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

    private readonly T* _value;
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

        _value = (T*)StringArena.Instance.AllocBytes(Unsafe.SizeOf<T>()).Ptr;
        if (T.Components == 1) LabelPlacement = LabelPlacement.Inline;
    }

    public ref T Value => ref *_value;
    
    public bool Draw()
    {
        if (_stepper.Tick()) *_value = _getter();

        DrawLabel();
        var changed = Style switch
        {
            InputStyle.Input => T.DrawFloatInput(StringId, _value, _format),
            InputStyle.Slider => T.DrawFloatSlider(StringId, _value, _format, Min, Max),
            InputStyle.Drag => T.DrawFloatDrag(StringId, _value, _format, Speed, Min, Max),
            _ => false
        };
        if (changed && ShouldTrigger())
        {
            _setter(*_value);
            return true;
        }

        return false;
    }
}

internal sealed unsafe class IntInput<T> : InputField where T : unmanaged, IInputNumeric<T>
{
    public readonly InputStyle Style;

    private readonly T* _value;
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

        _value = (T*)StringArena.Instance.AllocBytes(Unsafe.SizeOf<T>()).Ptr;
        if (T.Components == 1) LabelPlacement = LabelPlacement.Inline;
    }
    public ref T Value => ref *_value;

    public bool Draw()
    {
        if (_stepper.Tick()) *_value = _getter();

        DrawLabel();
        var changed = Style switch
        {
            InputStyle.Input => T.DrawIntInput(StringId, _value),
            InputStyle.Slider => T.DrawIntSlider(StringId, _value, Min, Max),
            InputStyle.Drag => T.DrawIntDrag(StringId, _value, Speed, Min, Max),
            _ => false
        };

        if (changed && ShouldTrigger())
        {
            _setter(*_value);
            return true;
        }

        return false;
    }
}

internal sealed unsafe class ColorInput : InputField
{
    public bool HasAlpha;

    private readonly Color4* _value;

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
        _value = (Color4*)StringArena.Instance.AllocBytes(Unsafe.SizeOf<Color4>()).Ptr;
    }
    
    public ref Color4 Value => ref *_value;

    public bool Draw()
    {
        if (_stepper.Tick()) *_value = _getter();

        DrawLabel();
        var changed = HasAlpha
            ? ImGui.ColorEdit4(StringId, &_value->R)
            : ImGui.ColorEdit3(StringId, &_value->R);

        if (changed && ShouldTrigger())
        {
            _setter(*_value);
            return true;
        }

        return false;
    }
}