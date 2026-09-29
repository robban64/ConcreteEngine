using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Lib.Inputs;

internal sealed unsafe class FloatInput<T> : InputField where T : unmanaged, IInputNumeric<T>
{
    public readonly InputStyle Style;

    public T Value;
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
    }

    public override bool Draw()
    {
        var strId = StringId;
        var value = Value;
        var changed = Style switch
        {
            InputStyle.Input => T.DrawFloatInput(strId._value, &value, _format),
            InputStyle.Slider => T.DrawFloatSlider(strId._value, &value, _format, Min, Max),
            InputStyle.Drag => T.DrawFloatDrag(strId._value, &value, _format, Speed, Min, Max),
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

internal sealed unsafe class IntInput<T> : InputField where T : unmanaged, IInputNumeric<T>
{
    public readonly InputStyle Style;

    public  T Value;
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
    }

    public override bool Draw()
    {
        var strId = StringId;
        var value = Value;

        var changed = Style switch
        {
            InputStyle.Input => T.DrawIntInput(strId._value, &value),
            InputStyle.Slider => T.DrawIntSlider(strId._value, &value, Min, Max),
            InputStyle.Drag => T.DrawIntDrag(strId._value, &value, Speed, Min, Max),
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