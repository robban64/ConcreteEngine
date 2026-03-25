using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal sealed unsafe class FloatField<T> : PropertyField<T> where T : unmanaged, IFloatValue
{
    private readonly delegate*<int, ref byte, ref float, ref byte, float, float, float, bool> _drawFunc;

    public float Speed, Min, Max;

    private NativeViewPtr<byte> _formatPtr;

    public FieldWidgetKind WidgetKind;

    public string Format
    {
        set => _formatPtr.Writer().Write(value);
    }

    protected override int SizeInBytes => T.Components * sizeof(float) + 8;

    public FloatField(string name, FieldWidgetKind widgetKind, Func<T>? getter = null, Action<T>? setter = null)
        : base(name, T.Components * sizeof(float) + 8, getter, setter)
    {
        _formatPtr = Allocator->AllocSlice(8);
        _formatPtr.Writer().Write("%.2f");

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
    protected override bool OnDraw()
    {
        var changed = _drawFunc((byte)T.Components, ref GetLabel(), ref Get().GetRef(), ref *_formatPtr.Ptr, Speed, Min,
            Max);
        return ShouldTrigger(changed);
    }
}

internal sealed unsafe class IntField<T> : PropertyField<T> where T : unmanaged, IIntValue
{
    private readonly delegate*<int, ref byte, ref int, float, int, int, bool> _drawFunc;
    public int Min, Max;
    public float Speed = 1f;
    public FieldWidgetKind WidgetKind;

    protected override int SizeInBytes => T.Components * sizeof(int);

    public IntField(string name, FieldWidgetKind widgetKind, Func<T>? getter = null, Action<T>? setter = null)
        : base(name, T.Components * sizeof(int), getter, setter)
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

    protected override bool OnDraw()
    {
        var changed = _drawFunc(T.Components, ref GetLabel(), ref Get().GetRef(), Speed, Min, Max);
        return ShouldTrigger(changed);
    }
}

internal sealed unsafe class ColorField(
    string name,
    bool hasAlpha,
    Func<Float4Value>? getter = null,
    Action<Float4Value>? setter = null)
    : PropertyField<Float4Value>(name, Float4Value.Components * sizeof(float), getter, setter)
{
    private readonly bool _hasAlpha = hasAlpha;
    protected override int SizeInBytes => Float4Value.Components * sizeof(float);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override bool OnDraw()
    {
        var changed = _hasAlpha
            ? ImGui.ColorEdit4(ref GetLabel(), ref Get().GetRef())
            : ImGui.ColorEdit3(ref GetLabel(), ref Get().GetRef());

        return ShouldTrigger(changed);
    }
}