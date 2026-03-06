using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal sealed unsafe class FloatField<T> : PropertyField<T> where T : unmanaged, IFloatValue
{
    public FieldWidgetKind WidgetKind;

    public float Speed, Min, Max;

    public String8Utf8 Format = "%.2f";

    private readonly delegate*<int, ref byte, ref float, ref byte, float, float, float, bool> _drawFunc;

    public FloatField(string name, FieldWidgetKind widgetKind, Func<T> getter, Action<T> setter)
        : base(name, getter, setter)
    {
        if (T.Components == 1) Layout = FieldLayout.Inline; 

        WidgetKind = widgetKind;
        _drawFunc = widgetKind switch
        {
            FieldWidgetKind.Input => &InputFieldDrawer.DrawInputFloat,
            FieldWidgetKind.Slider => &InputFieldDrawer.DrawSliderFloat,
            FieldWidgetKind.Drag => &InputFieldDrawer.DrawDragFloat,
            _ => throw new ArgumentOutOfRangeException(nameof(widgetKind), widgetKind, null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override bool OnDraw(ref T value)
    {
        FixedFormat = Format;
        ref var label = ref GetFixedLabel();
        return _drawFunc((byte)T.Components, ref label, ref value.GetRef(), ref FixedFormat.GetRef(), Speed, Min, Max);
    }
}

internal sealed unsafe class IntField<T> : PropertyField<T> where T : unmanaged, IIntValue
{
    public FieldWidgetKind WidgetKind;

    public int Min, Max;
    public float Speed = 1f;

    private readonly delegate*<int, ref byte, ref int, float, int, int, bool> _drawFunc;

    public IntField(string name, FieldWidgetKind widgetKind, Func<T> getter, Action<T> setter)
        : base(name, getter, setter)
    {
        if (T.Components == 1) Layout = FieldLayout.Inline; 

        WidgetKind = widgetKind;
        _drawFunc = widgetKind switch
        {
            FieldWidgetKind.Input => &InputFieldDrawer.DrawInputInt,
            FieldWidgetKind.Slider => &InputFieldDrawer.DrawSliderInt,
            FieldWidgetKind.Drag => &InputFieldDrawer.DrawDragInt,
            _ => throw new ArgumentOutOfRangeException(nameof(widgetKind), widgetKind, null)
        };
    }

    protected override bool OnDraw(ref T value)
    {
        return _drawFunc(T.Components, ref GetFixedLabel(), ref value.GetRef(), Speed, Min, Max);
    }
}

internal sealed class ColorField(string name, bool hasAlpha, Func<Float4Value> getter, Action<Float4Value> setter)
    : PropertyField<Float4Value>(name, getter, setter)
{
    protected override bool OnDraw(ref Float4Value value)
    {
        return hasAlpha
            ? ImGui.ColorEdit4(ref GetFixedLabel(), ref value.GetRef())
            : ImGui.ColorEdit3(ref GetFixedLabel(), ref value.GetRef());
    }

}