using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Lib.Field;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal sealed unsafe class FloatInput<T> : UiField where T : unmanaged, IFloatValue
{
    public T Value;
    public float Speed, Min, Max;
    public String8Utf8 Format = "%.2f";

    private readonly delegate*<int, byte*, float*, byte*, float, float, float, bool> _drawFunc;

    public FloatInput(string label, FieldKind widget) : base(label, widget)
    {
        _drawFunc = InputFieldDrawer.BindFloat(widget);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ref byte GetRawValue() => ref Unsafe.As<float, byte>(ref Value.GetRef());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Draw()
    {
        var value = Value;
        var format = Format;
        var label = ApplyLabelLayout(TextBuffers.GetWriter());
        var changed = _drawFunc(T.Components, label, (float*)&value, (byte*)&format, Speed, Min, Max);
        if (changed) Value = value;
        return changed && ShouldTrigger();
    }
}

internal sealed unsafe class IntInput<T> : UiField where T : unmanaged, IIntValue
{
    public T Value;
    public int Min, Max;
    public float Speed = 1f;

    private readonly delegate*<int, byte*, int*, float, int, int, bool> _drawFunc;

    public override ref byte GetRawValue() => ref Unsafe.As<int, byte>(ref Value.GetRef());

    public IntInput(string label, FieldKind widget) : base(label, widget)
    {
        _drawFunc = InputFieldDrawer.BindInt(widget);
    }

    public override bool Draw()
    {
        var value = Value;
        var label = ApplyLabelLayout(TextBuffers.GetWriter());
        var changed = _drawFunc(T.Components, label, (int*)&value, Speed, Min, Max);
        if (changed) Value = value;
        return changed && ShouldTrigger();
    }
}

internal sealed unsafe class ColorInput(string label) : UiField(label, FieldKind.Input)
{
    public Float4 Value;
    public bool HasAlpha;

    public override ref byte GetRawValue() => ref Unsafe.As<float, byte>(ref Value.GetRef());

    public override bool Draw()
    {
        var value = Value;
        var label = ApplyLabelLayout(TextBuffers.GetWriter());

        var changed = HasAlpha
            ? ImGui.ColorEdit4(label, (float*)&value)
            : ImGui.ColorEdit3(label, (float*)&value);

        if (changed) Value = value;
        return changed && ShouldTrigger();
    }
}