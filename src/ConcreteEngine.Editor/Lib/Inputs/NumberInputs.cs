using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class FloatInput<T> : InputField where T : unmanaged, IInputNumeric<T>
{
    public readonly InputStyle Style;

    private readonly T* _value;
    public float Speed, Min, Max;

    private readonly String8Utf8 _format;

    private readonly Action<T> _setter;

    public FloatInput(
        string label,
        InputStyle style,
        Action<T> setter,
        float speed = 1f,
        float min = 0,
        float max = 0,
        string format = "%.2f") : base(label, InputKind.Float)
    {
        Style = style;
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

    private readonly Action<T> _setter;

    public IntInput(
        string label,
        InputStyle style,
        Action<T> setter,
        float speed = 1f,
        int min = 0,
        int max = 0) : base(label, InputKind.Int)
    {
        Style = style;
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