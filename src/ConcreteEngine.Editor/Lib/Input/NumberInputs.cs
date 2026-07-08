using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Inspection;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal sealed unsafe class FloatInput<T> : InputField where T : unmanaged, IFloatValue
{
    public T Value;
    public float Speed, Min, Max;
    public String8Utf8 Format;

    private readonly delegate*< byte*, float*, byte*, float, float, float, bool> _drawFunc;

    public FloatInput(string label, InputStyle inputStyle, float speed = 1f, float min = 0, float max = 0,
        string format = "%.2f") : base(label, InputKind.Float)
    {
        _drawFunc = InputFieldDrawer.BindFloat2(inputStyle, T.Components);
        Format = format;
        Speed = speed;
        Min = min;
        Max = max;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ref byte GetRawValue() => ref Unsafe.As<float, byte>(ref Value.Ref());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Draw()
    {
        var value = Value;
        var format = Format;
        var label = ApplyLabelLayout(TextBuffers.GetWriter());
        var changed = _drawFunc(label, (float*)&value, (byte*)&format, Speed, Min, Max);
        if (changed) Value = value;
        return changed && ShouldTrigger();
    }
}

internal sealed unsafe class IntInput<T> : InputField where T : unmanaged, IIntValue
{
    public T Value;
    public int Min, Max;
    public float Speed;

    private readonly delegate*< byte*, int*, float, int, int, bool> _drawFunc;

    public override ref byte GetRawValue() => ref Unsafe.As<int, byte>(ref Value.Ref());

    public IntInput(string label, InputStyle inputStyle, float speed = 1f, int min = 0, int max = 0)
        : base(label, InputKind.Int)
    {
        _drawFunc = InputFieldDrawer.BindInt2(inputStyle, T.Components);
        Speed = speed;
        Min = min;
        Max = max;
    }

    public override bool Draw()
    {
        var value = Value;
        var label = ApplyLabelLayout(TextBuffers.GetWriter());
        var changed = _drawFunc(label, (int*)&value, Speed, Min, Max);
        if (changed) Value = value;
        return changed && ShouldTrigger();
    }
}

internal sealed unsafe class ColorInput(string label, bool hasAlpha = true) : InputField(label, InputKind.Color)
{
    public bool HasAlpha = hasAlpha;

    public Color4 Value;

    public override ref byte GetRawValue() => ref Unsafe.As<float, byte>(ref Value.R);

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