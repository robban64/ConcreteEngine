using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Lib.Field;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;


internal abstract class NumberInput<T>(string label, FieldWidgetKind widget) : UiElement(label, widget)
    where T : unmanaged, IFieldValue
{
    public T Value;
}

internal sealed unsafe class FloatInput<T> : NumberInput<T> where T : unmanaged, IFloatValue
{
    public T Value;
    public float Speed, Min, Max;
    public String8Utf8 Format = "%.2f";

    private readonly delegate*<int, byte*, float*, byte*, float, float, float, bool> _drawFunc;

    public FloatInput(string label, FieldWidgetKind widget) : base(label, widget)
    {
        _drawFunc = InputFieldDrawer.BindFloat(widget);
    }

    [SkipLocalsInit]
    public override bool Draw()
    {
        var buffer = stackalloc byte[LabelAllocCapacity];
        var value = Value;
        var format = Format;
        var label = DrawWriteLabel(buffer);
        
        if(Width > 0) ImGui.SetNextItemWidth(Width);
        var changed = _drawFunc(T.Components, label, (float*)&value, (byte*)&format, Speed, Min, Max);
        if (changed) Value = value;
        return changed && ShouldTrigger();
    }
}

internal sealed unsafe class IntInput<T> : NumberInput<T> where T : unmanaged, IIntValue
{
    public int Min, Max;
    public float Speed = 1f;

    private readonly delegate*<int, byte*, int*, float, int, int, bool> _drawFunc;

    public IntInput(string label, FieldWidgetKind widget) : base(label, widget)
    {
        _drawFunc = InputFieldDrawer.BindInt(widget);
    }

    [SkipLocalsInit]
    public override bool Draw()
    {
        var buffer = stackalloc byte[LabelAllocCapacity];
        var value = Value;
        var label = DrawWriteLabel(buffer);
        
        if(Width > 0) ImGui.SetNextItemWidth(Width);
        var changed = _drawFunc(T.Components, label, (int*)&value, Speed, Min, Max);
        if (changed) Value = value;
        return changed && ShouldTrigger();
    }
}
internal sealed unsafe class ColorInput(string label) : UiElement(label, FieldWidgetKind.Input)
{
    public bool HasAlpha;

    public Float4Value Value;

    [SkipLocalsInit]
    public override bool Draw()
    {
        var buffer = stackalloc byte[LabelAllocCapacity];
        var value = Value;
        var label = DrawWriteLabel(buffer);
        
        if(Width > 0) ImGui.SetNextItemWidth(Width);
        var changed = HasAlpha
            ? ImGui.ColorEdit4(label, (float*)&value)
            : ImGui.ColorEdit3(label, (float*)&value);

        if (changed) Value = value;
        return changed && ShouldTrigger();
    }
}
