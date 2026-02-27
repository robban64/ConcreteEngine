using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;


internal sealed class FloatField<T> : PropertyField<T> where T : unmanaged, IFloatValue
{
    public FieldWidgetKind WidgetKind;
    public float Speed, Min, Max;

    public String8Utf8 Format;

    private readonly InFunc<FloatDrawArg, bool> _drawWidget;

    public FloatField(string name, FieldWidgetKind widgetKind, Func<T> getter, Action<T> setter) 
        : base(name, getter, setter)
    {
        WidgetKind = widgetKind;
        _drawWidget = widgetKind switch
        {
            FieldWidgetKind.Input => static (in args) => InputFieldDrawer.DrawInputFloat<T>(in args),
            FieldWidgetKind.Slider => static (in args) => InputFieldDrawer.DrawSliderFloat<T>(in args),
            FieldWidgetKind.Drag => static (in args) => InputFieldDrawer.DrawDragFloat<T>(in args),
            _ => throw new ArgumentOutOfRangeException(nameof(widgetKind), widgetKind, null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override bool OnDraw()
    {
        ref var value = ref Get().GetRef();
        return _drawWidget(new FloatDrawArg(ref GetLabel(), ref value,  Format, Speed, Min, Max));
    }

}

internal sealed class IntField<T> : PropertyField<T> where T : unmanaged, IIntValue
{
        public FieldWidgetKind WidgetKind;

    public int Min, Max;
    public float Speed = 1f;

    private readonly InFunc<IntDrawArg, bool> _drawWidget;

    public IntField(string name, FieldWidgetKind widgetKind, Func<T> getter, Action<T> setter) 
        : base(name, getter, setter)
    {
        WidgetKind = widgetKind;
        _drawWidget = widgetKind switch
        {
            FieldWidgetKind.Input =>static (in args) => InputFieldDrawer.DrawInputInt<T>(in args),
            FieldWidgetKind.Slider => static (in args) =>InputFieldDrawer.DrawSliderInt<T>(in args),
            FieldWidgetKind.Drag =>static (in args) => InputFieldDrawer.DrawDragInt<T>(in args),
            _ => throw new ArgumentOutOfRangeException(nameof(widgetKind), widgetKind, null)
        };
    }

    protected override bool OnDraw()
    {
        return _drawWidget(new IntDrawArg(ref GetLabel(), ref Get().GetRef(), Min, Max, Speed));
    }
}

internal sealed class ColorField(string name, bool hasAlpha, Func<Float4Value> getter, Action<Float4Value> setter)
    : PropertyField<Float4Value>(name, getter, setter)
{
    protected override bool OnDraw()
    {
        ref var value = ref Get().GetRef();
        ref var label = ref GetLabel();
        return hasAlpha ? ImGui.ColorEdit4(ref label, ref value) : ImGui.ColorEdit3(ref label, ref value);
    }
}