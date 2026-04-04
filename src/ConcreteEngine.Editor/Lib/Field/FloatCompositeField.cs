using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Editor.Lib.Field;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct FloatCompositeEntry
{
    public byte* TextPtr;
    public readonly delegate*<int, ref byte, ref float, ref byte, float, float, float, bool> DrawFunc;
    public float Speed, Min, Max;

    public FloatCompositeEntry(
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

internal sealed unsafe class FloatCompositeField<T> : PropertyField<T> where T : unmanaged, IFloatValue
{
    private readonly FloatCompositeEntry[] _fields = new FloatCompositeEntry[T.Components];
    private NativeViewPtr<byte> _textPtr;
    private int _count;

    protected override int SizeInBytes => T.Components * 24;

    public FloatCompositeField(string name, Func<T> getter, Action<T> setter) : base(name, T.Components * 24, getter,
        setter)
    {
        Layout = FieldLayout.Inline;
        _textPtr = Allocator.AllocSlice(T.Components * 24);
    }

    protected override bool OnDraw()
    {
        var changed = false;
        ref var value = ref Get();
        for (var i = 0; i < T.Components; i++)
        {
            ref readonly var it = ref _fields[i];
            ref var v = ref Unsafe.Add(ref value.GetRef(), i);
            var hasChange = it.DrawFunc(1, ref *it.TextPtr, ref v, ref *(it.TextPtr + 16), it.Speed, it.Min, it.Max);
            changed |= ShouldTrigger(hasChange);
        }

        return changed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddField(FloatCompositeEntry entry)
    {
        ArgumentNullException.ThrowIfNull(_fields);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_count, T.Components);
        _fields[_count++] = entry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public FloatCompositeField<T> WithInput(string label, float min, float max, string format = "%.2f")
    {
        AddField(new FloatCompositeEntry(GetFieldSlicePtr(label, format), FieldWidgetKind.Input, 0, min, max));
        return this;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public FloatCompositeField<T> WithSlider(string label, float min, float max, string format = "%.2f")
    {
        AddField(new FloatCompositeEntry(GetFieldSlicePtr(label, format), FieldWidgetKind.Slider, 0, min, max));
        return this;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public FloatCompositeField<T> WithDrag(string label, float speed, float min, float max, string format = "%.2f")
    {
        AddField(new FloatCompositeEntry(GetFieldSlicePtr(label, format), FieldWidgetKind.Drag, speed, min, max));
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