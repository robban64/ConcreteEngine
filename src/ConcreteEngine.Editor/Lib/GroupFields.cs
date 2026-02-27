using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

[StructLayout(LayoutKind.Sequential)]
internal struct FloatGroupEntry(String16Utf8 label, float speed, float min, float max, string format = "%.2f")
{
    public FloatGroupEntry(String16Utf8 label, float min, float max, string format = "%.2f")
        : this(label, 0, min, max, format)
    {
    }

    public String16Utf8 Label = label;
    public String8Utf8 Format = format;
    public float Speed = speed, Min = min, Max = max;
}

internal sealed class FloatGroupField<T> : PropertyField<T> where T : unmanaged, IFloatValue
{
    private FloatGroupEntry[] _fields = new FloatGroupEntry[T.Components];
    private readonly InFunc<FloatDrawArg, bool> _drawWidget;

    public FloatGroupField(string name, FieldWidgetKind widgetKind, Func<T> getter,
        Action<T> setter) : base(name, getter, setter)
    {
        Layout = FieldLabelLayout.None;
        _drawWidget = widgetKind switch
        {
            FieldWidgetKind.Input => static (in args) => InputFieldDrawer.DrawInputFloat<Float1Value>(in args),
            FieldWidgetKind.Slider => static (in args) => InputFieldDrawer.DrawSliderFloat<Float1Value>(in args),
            FieldWidgetKind.Drag => static (in args) => InputFieldDrawer.DrawDragFloat<Float1Value>(in args),
            _ => throw new ArgumentOutOfRangeException(nameof(widgetKind), widgetKind, null)
        };
    }

    public void SetField(int component, FloatGroupEntry entry)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(component, T.Components);
        _fields[component] = entry;
    }

    protected override bool OnDraw()
    {
        var changed = false;
        ref var value = ref Get();
        for (int i = 0; i < _fields.Length; i++)
        {
            ImGui.PushID(i);
            ref var field = ref _fields[i];
            ref var fieldValue = ref Unsafe.Add(ref value.GetRef(), i);
            changed |= _drawWidget(new FloatDrawArg(ref field.Label.GetRef(), ref fieldValue, field.Format, field.Speed, field.Min, field.Max));
            ImGui.PopID();
        }

        return changed;
    }
}