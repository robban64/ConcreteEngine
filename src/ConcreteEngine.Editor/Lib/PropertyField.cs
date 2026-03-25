using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

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
        if (trigger.HasValue) field.Trigger = trigger.Value;
        return field;
    }
}

internal abstract unsafe class PropertyField
{
    public NativeViewPtr<String16Utf8> Name;
    public bool Visible = true;
    public bool IsBound {get; protected set; } = false;
    public FieldLayout Layout = FieldLayout.Top;
    public FieldTrigger Trigger;

    public FieldGetDelay Delay
    {
        get;
        set
        {
            value = field;
            FetchStepper.SetIntervalTicks((int)value, (int)value - 1);
        }
    } = FieldGetDelay.Low;

    protected FrameStepper FetchStepper = new((int)FieldGetDelay.Low);

    protected ArenaBlock* Allocator;

    protected PropertyField(string name, int sizeInBytes)
    {
        Allocator = TextBuffers.PersistentArena.Alloc(16 + sizeInBytes);
        Name = Allocator->AllocSlice(16).Reinterpret<String16Utf8>();
        *Name.Ptr = new String16Utf8(name);

    }

    public int TotalSizeInBytes => 16 + SizeInBytes;
    protected abstract int SizeInBytes { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref byte GetLabel() => ref Layout == FieldLayout.Inline ? ref Name[0].GetRef() : ref MemoryMarshal.GetReference("##input"u8);

    public bool Draw()
    {
        if (!Visible || !IsBound) return false;

        if (Layout == FieldLayout.Top)
        {
            ImGui.TextUnformatted(ref Name[0].GetRef());
            ImGui.Separator();
        }

        if (Layout != FieldLayout.None)
            ImGui.PushItemWidth(Layout == FieldLayout.Inline ? GuiTheme.FormItemInlineWidth : GuiTheme.FormItemWidth);

        var changed = OnDraw();

        if (Layout != FieldLayout.None) ImGui.PopItemWidth();

        if (changed) Set();
        return changed;
    }


    public abstract void Unbind();
    public abstract void Refresh();

    protected abstract void Set();
    protected abstract bool OnDraw();
}

internal abstract unsafe class PropertyField<T> : PropertyField
    where T : unmanaged, IFieldValue
{
    protected T* Value = null;
    private Func<T>? _getter;
    private Action<T>? _setter;

    public PropertyField(string name, int sizeInBytes, Func<T>? getter, Action<T>? setter) 
        : base(name, T.Components*sizeof(float) + sizeInBytes)
    {
        Value = Allocator->AllocSlice<T>();
        _getter = getter;
        _setter = setter;
        
        if(getter is not null && setter is not null)
            IsBound = true;
    }

    public void Bind(Func<T> getter, Action<T> setter)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);

        _getter = getter;
        _setter = setter;
        IsBound = true;
    }
    public override void Unbind()
    {
        _getter = null;
        _setter = null;
        IsBound = false;
    }

    public override void Refresh()
    {
        if (!IsBound || _getter is not { } getter) return;
        *Value = getter();
    }

    protected override void Set() => _setter?.Invoke(*Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref T Get()
    {
        if (IsBound && FetchStepper.Tick())
            *Value = _getter!();
        return ref *Value;
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
    /*
    public override bool Draw()
    {
        if (!Visible || !IsBound) return false;

        if (Layout == FieldLayout.Top)
        {
            ImGui.TextUnformatted(ref Name[0].GetRef());
            ImGui.Separator();
        }

        if (Layout != FieldLayout.None)
            ImGui.PushItemWidth(Layout == FieldLayout.Inline ? GuiTheme.FormItemInlineWidth : GuiTheme.FormItemWidth);

        var changed = OnDraw(ref Get());

        if (Layout != FieldLayout.None) ImGui.PopItemWidth();

        if (changed) Set();
        return changed;
    }
*/
}