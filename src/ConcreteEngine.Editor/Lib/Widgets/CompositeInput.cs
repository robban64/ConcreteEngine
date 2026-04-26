using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib.Field;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal sealed unsafe class FloatCompositeInput<T>(string label) : UiElement(label, FieldWidgetKind.Composite)
    where T : unmanaged, IFloatValue
{
    public T Value;
    private readonly ComponentEntry[] _fields = new ComponentEntry[T.Components];
    private int _count;

    [SkipLocalsInit]
    public override bool Draw()
    {
        var buffer = stackalloc byte[LabelAllocCapacity];
        var sw = new UnsafeSpanWriter(buffer, LabelAllocCapacity);

        var value = Value;
        var valuePtr = (float*)&value;

        var changed = false;
        var len = int.Min(T.Components, _count);
        ImGui.PushID(DrawId);
        for (var i = 0; i < len; i++, valuePtr++)
        {
            var it = _fields[i];
            var format = it.Format;
            var label = sw.Write(it.Name);

            ImGui.PushID(i);
            var hasChange = it.DrawFunc(1, label, valuePtr, (byte*)&format, it.Speed, it.Min, it.Max);
            changed |= hasChange && ShouldTrigger();
            ImGui.PopID();
        }
        ImGui.PopID();

        if (changed) Value = value;
        return changed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddField(ComponentEntry entry)
    {
        ArgumentNullException.ThrowIfNull(_fields);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_count, T.Components);

        _fields[_count] = entry;
        _count++;
    }

    public FloatCompositeInput<T> WithInput(string label, float min, float max, string format = "%.2f")
    {
        AddField(new ComponentEntry(label, FieldWidgetKind.Input, 0, min, max, format));
        return this;
    }

    public FloatCompositeInput<T> WithSlider(string label, float min, float max, string format = "%.2f")
    {
        AddField(new ComponentEntry(label, FieldWidgetKind.Slider, 0, min, max, format));
        return this;
    }

    public FloatCompositeInput<T> WithDrag(string label, float speed, float min, float max, string format = "%.2f")
    {
        AddField(new ComponentEntry(label, FieldWidgetKind.Drag, speed, min, max, format));
        return this;
    }


    private sealed class ComponentEntry
    {
        public readonly delegate*<int, byte*, float*, byte*, float, float, float, bool> DrawFunc;
        public readonly byte[] Name;
        public String8Utf8 Format;
        public float Speed, Min, Max;

        public ComponentEntry(
            string name,
            FieldWidgetKind widgetKind,
            float speed,
            float min,
            float max,
            string format)
        {
            Name = name.ToUtf8();
            Speed = speed;
            Min = min;
            Max = max;
            Format = format;
            DrawFunc = InputFieldDrawer.BindFloat(widgetKind);
        }
    }
}