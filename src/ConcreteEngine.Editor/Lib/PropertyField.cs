using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

public enum FieldGetDelay
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

public enum FieldLayout : byte
{
    None,
    Top,
    Inline,
}

public enum FieldTrigger : byte
{
    OnChange,
    AfterChange,
    AfterChangeDeactive
}

internal static class PropertyFieldExtensions
{
    public static T WithProperties<T>(
        this T field,
        FieldGetDelay delay = FieldGetDelay.Low,
        FieldLayout? layout = null,
        FieldTrigger? trigger = null) where T : PropertyField
    {
        field.Delay = delay;
        if (layout.HasValue) field.Layout = layout.Value;
        if(trigger.HasValue) field.Trigger = trigger.Value;
        return field;
    }
}

internal abstract class PropertyField
{
    protected static ReadOnlySpan<byte> DefaultInputLabel => "##input"u8;
    protected static ReadOnlySpan<byte> EmptyPlaceholder => "Empty"u8;

    private static int _idCounter = 1000;

    protected static UnsafeSpanWriter Sw = TextBuffers.GetWriter();

    //

    public readonly int Id = _idCounter++;

    public FieldLayout Layout = FieldLayout.Top;
    public FieldTrigger Trigger;

    internal String16Utf8 Name;

    protected FrameStepper FetchStepper = new((int)FieldGetDelay.Low);

    public FieldGetDelay Delay
    {
        get;
        set
        {
            value = field;
            FetchStepper.SetIntervalTicks((int)value, (int)value - 1);
        }
    } = FieldGetDelay.Low;


    protected PropertyField(string name)
    {
        Name = name;
        Name = new String16Utf8(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool ShouldTrigger(bool inputChange)
    {
        if (!inputChange) return false;
        return Trigger switch
        {
            FieldTrigger.OnChange => true,
            FieldTrigger.AfterChange => ImGui.IsItemDeactivatedAfterEdit(),
            FieldTrigger.AfterChangeDeactive => ImGui.IsItemDeactivatedAfterEdit() && !ImGui.IsItemActive(),
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref byte GetLabel()
    {
        return ref Layout == FieldLayout.Inline ? ref Name.GetRef() : ref MemoryMarshal.GetReference(DefaultInputLabel);
    }
}

internal abstract unsafe class PropertyField<T>(string name, Func<T> getter, Action<T> setter) : PropertyField(name)
    where T : unmanaged, IFieldValue
{
    [FixedAddressValueType] private static T _fixedValue;

    protected T Value;

    public void Refresh() => Value = getter();
    protected void Set() => setter(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref T Get()
    {
        if (FetchStepper.Tick()) Value = getter();
        return ref Value;
    }

    public bool Draw()
    {
        if (Layout == FieldLayout.Top)
        {
            ImGui.TextUnformatted(Sw.Write(ref Name.GetRef()));
            ImGui.Separator();
        }

        if (Layout != FieldLayout.None)
            ImGui.PushItemWidth(Layout == FieldLayout.Inline ? GuiTheme.FormItemInlineWidth : GuiTheme.FormItemWidth);

        ImGui.PushID(Id);
        ref var fixedValue = ref _fixedValue;
        fixedValue = Get();
        var changed = OnDraw(ref fixedValue);
        ImGui.PopID();

        if (Layout != FieldLayout.None) ImGui.PopItemWidth();

        if (changed)
        {
            Value = fixedValue;
            Set();
            return true;
        }
        return false;
    }

    protected abstract bool OnDraw(ref T value);
}