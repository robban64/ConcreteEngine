using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
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
        var label = Sw.Write(ref GetLabel());
        var format = Sw.Write(ref Format.GetRef(), 17);
        return _drawFunc((byte)T.Components,  ref label[0], ref value.GetRef(), ref format[0], Speed, Min, Max);
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
        var label = Sw.Write(ref GetLabel());
        return _drawFunc(T.Components, ref label[0], ref value.GetRef(), Speed, Min, Max);
    }
}

internal sealed class ColorField(string name, bool hasAlpha, Func<Float4Value> getter, Action<Float4Value> setter)
    : PropertyField<Float4Value>(name, getter, setter)
{
    protected override unsafe bool OnDraw(ref Float4Value value)
    {
        var label = Sw.Write(ref GetLabel());
        return hasAlpha
            ? ImGui.ColorEdit4(label, ref value.GetRef())
            : ImGui.ColorEdit3(label, ref value.GetRef());
    }

}