using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class FloatField<T> : PropertyField where T : unmanaged, IFloatValue
{
    private readonly delegate*<int, byte*, float*, byte*, float, float, float, bool> _drawFunc;

    public readonly PropertyFieldBinding<T> Binding;

    public float Speed, Min, Max;

    public String8Utf8 Format = "%.2f";

    public FloatField(string name, FieldWidgetKind widgetKind, Func<T>? getter = null, Action<T>? setter = null) : base(name)
    {
        Binding = new PropertyFieldBinding<T>();
        if (getter != null && setter != null)
            Binding.Bind(getter, setter);

        if (T.Components == 1) Layout = FieldLayout.Inline;

        WidgetKind = widgetKind;
        _drawFunc = InputFieldDrawer.BindFloat(widgetKind);
    }


    public void Bind(Func<T> getter , Action<T> setter) => Binding.Bind(getter, setter);

    public override IPropertyFieldBinding GetBinding() => Binding;
    public override void Refresh() => Binding.Refresh(Memory.GetValue<T>());
    protected override void Set() => Binding.Set(Memory.GetValue<T>());


    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override bool OnDraw()
    {
        var format = Format;
        var formatPtr = (byte*)&format;
        var value = Binding.Get(Memory.GetValue<T>());
        var changed = _drawFunc(T.Components, GetLabel(), (float*)value, formatPtr, Speed, Min, Max);
        return changed && ShouldTrigger();
    }

}

internal sealed unsafe class IntField<T> : PropertyField where T : unmanaged, IIntValue
{
    private readonly delegate*<int, byte*, int*, float, int, int, bool> _drawFunc;
    public readonly PropertyFieldBinding<T> Binding;
    public int Min, Max;
    public float Speed = 1f;

    public IntField(string name, FieldWidgetKind widgetKind, Func<T>? getter = null, Action<T>? setter = null) : base(name)
    {
        Binding = new PropertyFieldBinding<T>();
        if (getter != null && setter != null)
            Binding.Bind(getter, setter);

        if (T.Components == 1) Layout = FieldLayout.Inline;

        WidgetKind = widgetKind;
        _drawFunc = InputFieldDrawer.BindInt(widgetKind);
    }

    public void Bind(Func<T> getter , Action<T> setter) => Binding.Bind(getter, setter);

    public override IPropertyFieldBinding GetBinding() => Binding;
    public override void Refresh() => Binding.Refresh(Memory.GetValue<T>());
    protected override void Set() => Binding.Set(Memory.GetValue<T>());

    protected override bool OnDraw()
    {
        var label = GetLabel();
        var value = Binding.Get(Memory.GetValue<T>());
        var changed = _drawFunc(T.Components, label, (int*)value, Speed, Min, Max);
        return changed && ShouldTrigger();
    }

}

internal sealed unsafe class ColorField : PropertyField
{
    public bool HasAlpha;
    public readonly PropertyFieldBinding<Float4Value> Binding;

    public ColorField(string name, bool hasAlpha, Func<Float4Value>? getter = null, Action<Float4Value>? setter = null) : base(name)
    {
        HasAlpha = hasAlpha;
        Binding = new PropertyFieldBinding<Float4Value>();
        if (getter != null && setter != null)
            Binding.Bind(getter, setter);
    }

    public void Bind(Func<Float4Value> getter , Action<Float4Value> setter) => Binding.Bind(getter, setter);

    public override IPropertyFieldBinding GetBinding() => Binding;
    public override void Refresh() => Binding.Refresh(Memory.GetValue<Float4Value>());
    protected override void Set() => Binding.Set(Memory.GetValue<Float4Value>());


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override bool OnDraw()
    {
        var label = GetLabel();
        var value = (float*)Binding.Get(Memory.GetValue<Float4Value>());

        var changed = HasAlpha
            ? ImGui.ColorEdit4(label, value)
            : ImGui.ColorEdit3(label, value);

        return changed && ShouldTrigger();
    }
}
/*

internal sealed unsafe class ColorField : PropertyField
{
    public bool HasAlpha;
    public readonly PropertyFieldBinding<Float4Value> Binding;
    public Float4Value Value;

    public ColorField(string name, bool hasAlpha, Func<Float4Value>? getter = null, Action<Float4Value>? setter = null) : base(name)
    {
        HasAlpha = hasAlpha;
        Binding = new PropertyFieldBinding<Float4Value>();
        if (getter != null && setter != null)
            Binding.Bind(getter, setter);
    }

    public void Bind(Func<Float4Value> getter , Action<Float4Value> setter) => Binding.Bind(getter, setter);

    public override IPropertyFieldBinding GetBinding() => Binding;
    public override void Refresh() => Binding.Refresh(ref Value);
    protected override void Set() => Binding.Set(ref Value);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override bool OnDraw()
    {
        var label = GetLabel();
        ref var value = ref Value;
        var v = Binding.Get(ref Value);

        var changed = HasAlpha
            ? ImGui.ColorEdit4(label, (float*)&v)
            : ImGui.ColorEdit3(label, (float*)&v);
        
        if(changed) value = v;

        return ShouldTrigger(changed);
    }
}
*/