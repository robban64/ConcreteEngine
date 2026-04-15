using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class FloatField<T> : PropertyField<T> where T : unmanaged, IFloatValue
{
    private readonly delegate*<int, ref byte, ref float, ref byte, float, float, float, bool> _drawFunc;

    public float Speed, Min, Max;

    private String8Utf8* _formatPtr;

    public string Format
    {
        set => *_formatPtr = new String8Utf8(value);
    }


    public FloatField(string name, FieldWidgetKind widgetKind, Func<T>? getter = null, Action<T>? setter = null)
        : base(name, T.Components * sizeof(float) + 8, getter, setter)
    {
        _formatPtr = (String8Utf8*)Allocator.AllocSlice(8).Ptr;
        *_formatPtr = new String8Utf8("%.2f");

        if (T.Components == 1) Layout = FieldLayout.Inline;

        WidgetKind = widgetKind;
        _drawFunc = InputFieldDrawer.BindFloat(widgetKind);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override bool OnDraw()
    {
        ref var label = ref *GetLabel();
        ref var value = ref Get().GetRef();
        var changed = _drawFunc(T.Components, ref label, ref value, ref *(byte*)_formatPtr, Speed, Min, Max);
        return ShouldTrigger(changed);
    }
}

internal sealed unsafe class IntField<T> : PropertyField<T> where T : unmanaged, IIntValue
{
    private readonly delegate*<int, ref byte, ref int, float, int, int, bool> _drawFunc;
    public int Min, Max;
    public float Speed = 1f;

    public IntField(string name, FieldWidgetKind widgetKind, Func<T>? getter = null, Action<T>? setter = null)
        : base(name, T.Components * sizeof(int), getter, setter)
    {
        if (T.Components == 1) Layout = FieldLayout.Inline;

        WidgetKind = widgetKind;
        _drawFunc = InputFieldDrawer.BindInt(widgetKind);
    }

    protected override bool OnDraw()
    {
        ref var label = ref *GetLabel();
        ref var value = ref Get().GetRef();
        var changed = _drawFunc(T.Components, ref label, ref value, Speed, Min, Max);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override bool OnDraw()
    {
        ref var label = ref *GetLabel();
        ref var value = ref Get().GetRef();

        var changed = hasAlpha
            ? ImGui.ColorEdit4(ref label, ref value)
            : ImGui.ColorEdit3(ref label, ref value);

        return ShouldTrigger(changed);
    }
}