using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Core.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal sealed unsafe class FloatInput<T> : InputField where T : unmanaged, IFloatValue
{
    public readonly InputStyle Style;
    
    public float Speed, Min, Max;

    public String8Utf8 Format;

    private readonly InputDrawer _drawer;
    private readonly Func<T> _getter;
    private readonly Action<T> _setter;

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
        _drawer = InputDrawer.Get(style);
        _getter = getter;
        _setter = setter;
        Format = format;
        Speed = speed;
        Min = min;
        Max = max;
    }

    public bool Draw()
    {
        var value = _getter();
        var format = Format;
        var label = ApplyLabelLayout(TextBuffers.GetWriter());
        var changed = _drawer.DrawFloat(T.Components, label, (float*)&value, (byte*)&format, Speed, Min, Max);
        if (changed && ShouldTrigger())
        {
            _setter(value);
            return true;
        }
        return false;
    }
}

internal sealed unsafe class IntInput<T> : InputField where T : unmanaged, IIntValue
{
    public readonly InputStyle Style;
    
    public int Min, Max;
    public float Speed;

    private readonly InputDrawer _drawer;

    private readonly Func<T> _getter;
    private readonly Action<T> _setter;

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
        _drawer = InputDrawer.Get(style);
        _getter = getter;
        _setter = setter;
        Speed = speed;
        Min = min;
        Max = max;

    }

    public bool Draw()
    {
        var value = _getter();
        var label = ApplyLabelLayout(TextBuffers.GetWriter());
        var changed = _drawer.DrawInt(T.Components, label, (int*)&value, Speed, Min, Max);
        if (changed && ShouldTrigger())
        {
            _setter(value);
            return true;
        }
        return false;
    }
}

internal sealed unsafe class ColorInput : InputField
{
    public bool HasAlpha;

    private readonly Func<Color4> _getter;
    private readonly Action<Color4> _setter;

    public ColorInput(string label, Func<Color4> getter, Action<Color4> setter, bool hasAlpha = true) 
        : base(label, InputKind.Color)
    {
        _getter = getter;
        _setter = setter;
        HasAlpha = hasAlpha;
    }

    public bool Draw()
    {
        var value = _getter();
        var label = ApplyLabelLayout(TextBuffers.GetWriter());
        var changed = HasAlpha
            ? ImGui.ColorEdit4(label, &value.R)
            : ImGui.ColorEdit3(label, &value.R);

        if (changed && ShouldTrigger())
        {
            _setter(value);
            return true;
        }

        return false;
    }
}