using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

public enum PropertyGetDelay
{
    None = 0,
    Low = 4,
    Medium = 40,
    High = 160,
    VeryHigh = 1440
}

public enum FieldWidgetKind : byte
{
    Input,
    Slider,
    Drag,
    Combo
}

public enum FieldLabelLayout : byte
{
    None,
    Top,
    Inline,
}

internal static class PropertyFieldExtensions
{
    public static T WithDelay<T>(this T field, PropertyGetDelay delay) where T : PropertyField
    {
        field.Delay = delay;
        return field;
    }
}

internal abstract class PropertyField
{
    protected static ReadOnlySpan<byte> DefaultInputLabel => "##input"u8;
    protected static ReadOnlySpan<byte> EmptyPlaceholder => "Empty"u8;


    private static int _idCounter = 1000;
    //

    public readonly int Id = _idCounter++;

    public FieldLabelLayout Layout = FieldLabelLayout.Top;

    internal String16Utf8 Name;

    protected FrameStepper Stepper = new((int)PropertyGetDelay.Low);
    
    public PropertyGetDelay Delay
    {
        get;
        set
        {
            value = field;
            Stepper.SetIntervalTicks((int)value, (int)value - 1);
        }
    } = PropertyGetDelay.Low;


    protected PropertyField(string name)
    {
        Name = name;
        Name = new String16Utf8(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref byte GetLabel()
    {
        return ref Layout == FieldLabelLayout.Inline
            ? ref Name.GetRef()
            : ref MemoryMarshal.GetReference(DefaultInputLabel);
    }
}

internal abstract class PropertyField<T>(string name, Func<T> getter, Action<T> setter)
    : PropertyField(name) where T : unmanaged, IFieldValue
{
    protected T Value;

    public readonly Func<T> Getter = getter;
    public readonly Action<T> Setter = setter;

    public void Refresh() => Value = Getter();
    protected void Set() => Setter(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref T Get()
    {
        if (Stepper.Tick()) Value = Getter();
        return ref Value;
    }

    public bool Draw(float width = 0f)
    {
        if (Layout == FieldLabelLayout.Top)
        {
            ImGui.TextUnformatted(ref Name.GetRef());
            ImGui.Separator();
        }

        if (width > 0) ImGui.SetNextItemWidth(width);

        ImGui.PushID(Id);
        var changed = OnDraw();
        ImGui.PopID();
        if (changed) Set();
        return changed;
    }

    protected abstract bool OnDraw();
}