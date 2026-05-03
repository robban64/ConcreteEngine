using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Editor.Lib.Field;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct FloatCompositeEntry
{
    public readonly delegate*<int,  byte*,  float*,  byte*, float, float, float, bool> DrawFunc;
    public RangeU16 TextHandle;
    public float Speed, Min, Max;

    public FloatCompositeEntry(
        FieldWidgetKind widgetKind,
        float speed,
        float min,
        float max)
    {
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

internal sealed class FloatCompositeDef(string label, string format)
{
    public string Label = label;
    public string Format = format;
}

internal sealed unsafe class FloatCompositeField<T> : PropertyField where T : unmanaged, IFloatValue
{
    private const int CustomDataStride = 32;
    private const int LabelStride = 24;

    public readonly PropertyFieldBinding<T> Binding;
    
    private readonly FloatCompositeDef[] _fieldDef = new FloatCompositeDef[T.Components];
    private readonly FloatCompositeEntry[] _fields = new FloatCompositeEntry[T.Components];
    private int _count;

    protected override int CustomDataSize => T.Components * CustomDataStride;

    public FloatCompositeField(string name, Func<T> getter, Action<T> setter) : base(name)
    {
        Binding = new PropertyFieldBinding<T>();
        Layout = FieldLayout.Inline;
        
        Bind(getter, setter);
    }

    protected override void OnAllocate(FieldMemory memory)
    {
        for (int i = 0; i < _count; i++)
        {
            var def = _fieldDef[i];
            
            var slice = memory.CustomData.SliceFrom(i * CustomDataStride);
            slice.Clear();
            var sw = slice.Writer();
            sw.Write(def.Label);
            sw.SetCursor(LabelStride);
            sw.Append(def.Format).End();
            _fields[i].TextHandle = slice.AsRange16();
        }
    }

    public void Bind(Func<T> getter , Action<T> setter) => Binding.Bind(getter, setter);

    public override IPropertyFieldBinding GetBinding() => Binding;
    public override void Refresh() => Binding.Refresh(Memory.GetValue<T>());
    protected override void Set() => Binding.Set(Memory.GetValue<T>());

    protected override bool OnDraw()
    {
        var changed = false;
         var value =  Binding.Get(Memory.GetValue<T>());
        for (var i = 0; i < T.Components; i++)
        {
            ref readonly var it = ref _fields[i];
            var v = (float*)value + i;
            var text = Memory.CustomData.Slice(it.TextHandle);
            var hasChange = it.DrawFunc(1, text, v, text + LabelStride, it.Speed, it.Min, it.Max);
            changed |= hasChange && ShouldTrigger();
        }

        return changed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddField(string label, string format, FloatCompositeEntry entry)
    {
        ArgumentNullException.ThrowIfNull(_fields);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_count, T.Components);
        
        _fieldDef[_count] = new FloatCompositeDef(label, format);
        _fields[_count] = entry;
        _count++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public FloatCompositeField<T> WithInput(string label, float min, float max, string format = "%.2f")
    {
        AddField(label,format,new FloatCompositeEntry( FieldWidgetKind.Input, 0, min, max));
        return this;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public FloatCompositeField<T> WithSlider(string label, float min, float max, string format = "%.2f")
    {
        AddField(label,format,new FloatCompositeEntry( FieldWidgetKind.Slider, 0, min, max));
        return this;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public FloatCompositeField<T> WithDrag(string label, float speed, float min, float max, string format = "%.2f")
    {
        AddField(label,format,new FloatCompositeEntry( FieldWidgetKind.Drag, speed, min, max));
        return this;
    }

}