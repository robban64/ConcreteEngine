using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Lib;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct FloatGroupEntry
{
    public byte* TextPtr;
    public readonly delegate*<int, ref byte, ref float, ref byte, float, float, float, bool> DrawFunc;
    public float Speed, Min, Max;

    public FloatGroupEntry(
        byte* textPtr,
        FieldWidgetKind widgetKind,
        float speed,
        float min,
        float max)
    {
        TextPtr = textPtr;
        Speed = speed;
        Min = min;
        Max = max;
        DrawFunc = widgetKind switch
        {
            FieldWidgetKind.Input => &InputFieldDrawer.DrawInputFloat,
            FieldWidgetKind.Slider => &InputFieldDrawer.DrawSliderFloat,
            FieldWidgetKind.Drag => &InputFieldDrawer.DrawDragFloat,
            _ => throw new ArgumentOutOfRangeException(nameof(widgetKind), widgetKind, null)
        };
    }
}

internal sealed unsafe class FloatGroupField<T> : PropertyField<T> where T : unmanaged, IFloatValue
{
    private readonly FloatGroupEntry[] _fields = new FloatGroupEntry[T.Components];
    private NativeViewPtr<byte> _textPtr;
    private int _count;

    protected override int SizeInBytes => T.Components * 24;

    public FloatGroupField(string name, Func<T> getter, Action<T> setter) : base(name, T.Components * 24, getter,
        setter)
    {
        Layout = FieldLayout.Inline;
        _textPtr = Allocator->AllocSlice(T.Components * 24);
    }

    protected override bool OnDraw()
    {
        var changed = false;
        ref var value = ref Get();
        for (var i = 0; i < T.Components; i++)
        {
            ref readonly var it = ref _fields[i];
            var label = it.TextPtr;
            var format = it.TextPtr + 16;
            ref var fieldValue = ref Unsafe.Add(ref value.GetRef(), i);
            var hasChange = it.DrawFunc(1, ref *label, ref fieldValue, ref *format, it.Speed, it.Min, it.Max);
            /*
            var hasChange = it.WidgetKind switch
            {
                FieldWidgetKind.Input =>
                    ImGui.InputFloat(label, ref fieldValue, format),
                FieldWidgetKind.Slider =>
                    ImGui.SliderFloat(label, ref fieldValue, it.Min, it.Max, format),
                FieldWidgetKind.Drag =>
                    ImGui.DragFloat(label, ref fieldValue, it.Speed, it.Min, it.Max, format),
                _ => false
            };*/
            changed |= ShouldTrigger(hasChange);
        }

        return changed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddField(FloatGroupEntry entry)
    {
        ArgumentNullException.ThrowIfNull(_fields);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_count, T.Components);
        _fields[_count++] = entry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public FloatGroupField<T> WithInput(string label, float min, float max, string format = "%.2f")
    {
        AddField(new FloatGroupEntry(GetFieldSlicePtr(label, format), FieldWidgetKind.Input, 0, min, max));
        return this;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public FloatGroupField<T> WithSlider(string label, float min, float max, string format = "%.2f")
    {
        AddField(new FloatGroupEntry(GetFieldSlicePtr(label, format), FieldWidgetKind.Slider, 0, min, max));
        return this;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public FloatGroupField<T> WithDrag(string label, float speed, float min, float max, string format = "%.2f")
    {
        AddField(new FloatGroupEntry(GetFieldSlicePtr(label, format), FieldWidgetKind.Drag, speed, min, max));
        return this;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private byte* GetFieldSlicePtr(string label, string format)
    {
        var slice = _textPtr.SliceFrom(_count * 24);
        var sw = slice.Writer();
        sw.Write(label);
        sw.SetCursor(16);
        sw.Append(format).End();
        return slice;
    }
}