using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

public abstract class InputValueField<T>(string name, Func<T> getter, Action<T> setter)
    : PropertyField(name) where T : unmanaged
{
    public T Value;

    internal String8Utf8 FormatUtf8;

    public string Format
    {
        get;
        set
        {
            value = field;
            FormatUtf8 = new String8Utf8(value);
        }
    } = string.Empty;

    public void Refresh() => getter();
    public void Set() => setter(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref T Get()
    {
        if (Stepper.Tick()) Value = getter();
        return ref Value;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref byte ApplyDrawStyle(bool vertical)
    {
        if (!vertical) return ref NameUtf8.GetRef();

        ImGui.TextUnformatted(ref NameUtf8.GetRef());
        ImGui.Separator();
        return ref DefaultInputLabel.GetRef();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Draw(bool vertical = true, float width = 0)
    {
        ref var label = ref ApplyDrawStyle(vertical);
        ref var format = ref FormatUtf8.IsEmpty ? ref Unsafe.NullRef<byte>() : ref FormatUtf8.GetRef();

        if (width > 0) ImGui.SetNextItemWidth(width);
        ImGui.PushID(Id);
        var changed = OnDraw(ref label, ref Get(), ref format);
        ImGui.PopID();

        if (changed)
        {
            Set();
        }

        return changed;
    }
    
    protected abstract bool OnDraw(ref byte label, ref T value, ref byte format);
}

public sealed class IntInputValueField<T>(string name, Func<T> getter, Action<T> setter)
    : InputValueField<T>(name, getter, setter) where T : unmanaged, IIntValue
{
    protected override bool OnDraw(ref byte label, ref T v, ref byte format)
    {
        return T.Components switch
        {
            1 => ImGui.InputInt(ref label, ref v.GetRef()),
            2 => ImGui.InputInt2(ref label, ref v.GetRef()),
            3 => ImGui.InputInt3(ref label, ref v.GetRef()),
            _ => throw new NotImplementedException(),
        };
    }
}

public sealed class IntSliderValueField<T>(string name, Func<T> getter, Action<T> setter)
    : InputValueField<T>(name, getter, setter) where T : unmanaged, IIntValue
{
    public int Min;
    public int Max;

    protected override bool OnDraw(ref byte label, ref T v, ref byte format)
    {
        return T.Components switch
        {
            1 => ImGui.SliderInt(ref label, ref v.GetRef(), Min, Max, ref format),
            2 => ImGui.SliderInt2(ref label, ref v.GetRef(), Min, Max, ref format),
            3 => ImGui.SliderInt3(ref label, ref v.GetRef(), Min, Max, ref format),
            _ => throw new NotImplementedException(),
        };
    }
}

public sealed class IntDragValueField<T>(string name, Func<T> getter, Action<T> setter)
    : InputValueField<T>(name, getter, setter)
    where T : unmanaged, IIntValue
{
    public float Speed;
    public int Min;
    public int Max;

    protected override bool OnDraw(ref byte label, ref T v, ref byte format)
    {
        return T.Components switch
        {
            1 => ImGui.DragInt(ref label, ref v.GetRef(), Speed, Min, Max, ref format),
            2 => ImGui.DragInt2(ref label, ref v.GetRef(), Speed, Min, Max, ref format),
            3 => ImGui.DragInt3(ref label, ref v.GetRef(), Speed, Min, Max, ref format),
            _ => throw new NotImplementedException(),
        };
    }
}

public sealed class FloatInputValueField<T>(string name, Func<T> getter, Action<T> setter)
    : InputValueField<T>(name, getter, setter) where T : unmanaged, IFloatValue
{
    protected override bool OnDraw(ref byte label, ref T v, ref byte format)
    {
        return T.Components switch
        {
            1 => ImGui.InputFloat(ref label, ref v.GetRef(), ref format),
            2 => ImGui.InputFloat2(ref label, ref v.GetRef(), ref format),
            3 => ImGui.InputFloat3(ref label, ref v.GetRef(), ref format),
            _ => throw new NotImplementedException(),
        };
    }
}

public sealed class FloatSliderField<T>(string name, Func<T> getter, Action<T> setter)
    : InputValueField<T>(name, getter, setter) where T : unmanaged, IFloatValue
{
    public float Speed;
    public float Min;
    public float Max;

    protected override bool OnDraw(ref byte label, ref T v, ref byte format)
    {
        return T.Components switch
        {
            1 => ImGui.SliderFloat(ref label, ref v.GetRef(), Min, Max, ref format),
            2 => ImGui.SliderFloat2(ref label, ref v.GetRef(), Min, Max, ref format),
            3 => ImGui.SliderFloat3(ref label, ref v.GetRef(), Min, Max, ref format),
            _ => throw new NotImplementedException(),
        };
    }
}

public sealed class FloatDragField<T>(string name, Func<T> getter, Action<T> setter)
    : InputValueField<T>(name, getter, setter) where T : unmanaged, IFloatValue
{
    public float Speed;
    public float Min;
    public float Max;

    protected override bool OnDraw(ref byte label, ref T v, ref byte format)
    {
        return T.Components switch
        {
            1 => ImGui.DragFloat(ref label, ref v.GetRef(), Speed, Min, Max, ref format),
            2 => ImGui.DragFloat2(ref label, ref v.GetRef(), Speed, Min, Max, ref format),
            3 => ImGui.DragFloat3(ref label, ref v.GetRef(), Speed, Min, Max, ref format),
            _ => throw new NotImplementedException(),
        };
    }
}

public sealed class ColorInputField(string name, bool hasAlpha, Func<Color4> getter, Action<Color4> setter)
    : InputValueField<Color4>(name, getter, setter)
{
    public float Speed;
    public float Min;
    public float Max;

    protected override bool OnDraw(ref byte label, ref Color4 v, ref byte format)
    {
        return hasAlpha ? ImGui.ColorEdit4(ref label, ref v.R) : ImGui.ColorEdit3(ref label, ref v.R);
    }
}