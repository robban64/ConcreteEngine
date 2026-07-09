using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Inspection;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal sealed unsafe class FloatInput : InputField
{
    public int Components;
    public float Speed, Min, Max;
    public String8Utf8 Format;

    private readonly delegate*<byte*, float*, byte*, float, float, float, bool> _drawFunc;

    public FloatInput(string label, int components, InputStyle inputStyle, float speed = 1f, float min = 0, float max = 0,
        string format = "%.2f") : base(label, InputKind.Float)
    {
        if(components < 0 || components > 4) throw new ArgumentOutOfRangeException(nameof(components));
        _drawFunc = InputFieldDrawer.BindFloat2(inputStyle, components);
        Components = components;
        Format = format;
        Speed = speed;
        Min = min;
        Max = max;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Draw(float* value)
    {
        var format = Format;
        var label = ApplyLabelLayout(TextBuffers.GetWriter());
        var changed = _drawFunc(label, value, (byte*)&format, Speed, Min, Max);
        return changed && ShouldTrigger();
    }
}

internal sealed unsafe class IntInput<T> : InputField where T : unmanaged, IIntValue
{
    public T Value;
    public int Min, Max;
    public float Speed;

    private readonly delegate*< byte*, int*, float, int, int, bool> _drawFunc;

    public IntInput(string label, InputStyle inputStyle, float speed = 1f, int min = 0, int max = 0)
        : base(label, InputKind.Int)
    {
        _drawFunc = InputFieldDrawer.BindInt2(inputStyle, T.Components);
        Speed = speed;
        Min = min;
        Max = max;
    }

    public  bool Draw<T>(T value) where T : unmanaged, IIntValue
    {
        var label = ApplyLabelLayout(TextBuffers.GetWriter());
        var changed = _drawFunc(label, (int*)&value, Speed, Min, Max);
        return changed && ShouldTrigger();
    }
}

internal sealed unsafe class ColorInput(string label, bool hasAlpha = true) : InputField(label, InputKind.Color)
{
    public bool HasAlpha = hasAlpha;

    public bool Draw(Color4 value)
    {
        var label = ApplyLabelLayout(TextBuffers.GetWriter());

        var changed = HasAlpha
            ? ImGui.ColorEdit4(label, (float*)&value)
            : ImGui.ColorEdit3(label, (float*)&value);

        return changed && ShouldTrigger();
    }
}