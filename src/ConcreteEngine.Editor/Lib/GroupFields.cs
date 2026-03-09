using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

[StructLayout(LayoutKind.Sequential)]
internal struct FloatGroupEntry(
    String16Utf8 label,
    FieldWidgetKind widgetKind,
    float speed,
    float min,
    float max,
    string format = "%.2f")
{
    public FloatGroupEntry(String16Utf8 label, FieldWidgetKind widgetKind, float min, float max, string format = "%.2f")
        : this(label, widgetKind, 0, min, max, format)
    {
    }

    public String16Utf8 Label = label;
    public String8Utf8 Format = format;
    public float Speed = speed, Min = min, Max = max;
    public FieldWidgetKind WidgetKind = widgetKind;

    public static FloatGroupEntry Input(String16Utf8 label, string format = "%.2f") =>
        new(label, FieldWidgetKind.Slider, 0, 0, format);

    public static FloatGroupEntry Slider(String16Utf8 label, float min, float max, string format = "%.2f") =>
        new(label, FieldWidgetKind.Slider, min, max, format);

    public static FloatGroupEntry Drag(String16Utf8 label, float speed, float min, float max, string format = "%.2f") =>
        new(label, FieldWidgetKind.Slider, speed, min, max, format);
}

internal sealed unsafe class FloatGroupField<T> : PropertyField<T> where T : unmanaged, IFloatValue
{
    private readonly FloatGroupEntry[] _fields = new FloatGroupEntry[T.Components];

    private int _count;

    public FloatGroupField(string name, Func<T> getter, Action<T> setter) : base(name, getter, setter)
    {
        Layout = FieldLayout.Inline;
    }

    protected override bool OnDraw(ref T value)
    {
        var changed = false;
        for (var i = 0; i < T.Components; i++)
        {
            ref var field = ref _fields[i];
            var label = Sw.Write(ref field.Label.GetRef());
            var format = Sw.Write(ref field.Format.GetRef(), 17);
            ref var fieldValue = ref Unsafe.Add(ref value.GetRef(), i);
            changed |= field.WidgetKind switch
            {
                FieldWidgetKind.Input =>
                    ImGui.InputFloat(label, ref fieldValue, format),
                FieldWidgetKind.Slider =>
                    ImGui.SliderFloat(label, ref fieldValue, field.Min, field.Max, format),
                FieldWidgetKind.Drag =>
                    ImGui.DragFloat(label, ref fieldValue, field.Speed, field.Min, field.Max, format),
                _ => false
            };
        }

        return changed;
    }


    public void AddField(FloatGroupEntry entry)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_count, T.Components);
        _fields[_count++] = entry;
    }

    public FloatGroupField<T> WithInput(string label, float min, float max)
    {
        AddField(FloatGroupEntry.Slider(label, min, max));
        return this;
    }

    public FloatGroupField<T> WithSlider(string label, float min, float max, string format = "%.2f")
    {
        AddField(FloatGroupEntry.Slider(label, min, max, format));
        return this;
    }

    public FloatGroupField<T> WithDrag(string label, float speed, float min, float max, string format = "%.2f")
    {
        AddField(FloatGroupEntry.Drag(label, speed, min, max, format));
        return this;
    }
}