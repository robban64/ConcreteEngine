using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

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

internal interface IPropertyFieldBinding
{
    int ValueStride { get; }
    void SetFetchInterval(int intervalTicks, int ticks = 0);
    void Unbind();
}

internal unsafe sealed class PropertyFieldBinding<T> : IPropertyFieldBinding where T : unmanaged, IFieldValue
{
    private Func<T>? _getter;
    private Action<T>? _setter;
    private FrameStepper _fetchStepper;

    public bool IsBound => _getter != null && _setter != null;

    public int ValueStride => Unsafe.SizeOf<T>();

    public void SetFetchInterval(int intervalTicks, int ticks = 0) => _fetchStepper.SetIntervalTicks(intervalTicks, ticks);

    public void Bind(Func<T> getter, Action<T> setter)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);

        _getter = getter;
        _setter = setter;
    }

    public void Unbind()
    {
        _getter = null;
        _setter = null;
    }

    public void Refresh(T* value)
    {
        if (_getter is not { } getter) return;
        *value = getter();
    }

    public void Set(T* value)
    {
        if (_setter is not { } setter) return;
        setter.Invoke(*value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* Get(T* value)
    {
        if (_getter is { } getter && _fetchStepper.Tick())
            *value = getter();
        return value;
    }

}
internal abstract unsafe class PropertyField
{
    private static int IdCounter = 2000;

    protected readonly int DrawId;
    public readonly string Name;
    public FieldMemory Memory;
    public bool Visible = true;
    public FieldLayout Layout = FieldLayout.Top;
    public FieldTrigger Trigger;
    public FieldWidgetKind WidgetKind;
    public FieldGetDelay Delay
    {
        get;
        set
        {
            value = field;
            GetBinding().SetFetchInterval((int)value, (int)value - 1);
        }
    } = FieldGetDelay.Low;

    protected virtual int CustomDataSize { get; } = 0;

    protected PropertyField(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        DrawId = IdCounter++;
        Name = name;
        Memory = new FieldMemory();
        /*
        Allocator = TextBuffers.PersistentArena.Alloc(40 + sizeInBytes);
        var namePtr = Allocator.AllocSlice(40);
        var sw = namePtr.Writer();
        sw.Write(name);
        sw.SetCursor(24);
        sw.Append("##input").Append(DrawId).End();
        NamePtr = namePtr;*/
    }

    public abstract IPropertyFieldBinding GetBinding();
    public abstract void Refresh();
    protected abstract void Set();
    protected abstract bool OnDraw();
    protected virtual void OnAllocate(FieldMemory memory){}

    public void Allocate(ArenaAllocator allocator)
    {
        if(!Memory.IsNull) throw new InvalidOperationException("Allocate invoked multiple times");
        var stride = GetBinding().ValueStride;
        Memory.Allocate(allocator.AllocBuilder(), DrawId, Name, stride, CustomDataSize);
        OnAllocate(Memory);
    }


    public bool Draw()
    {
        if (!Visible || Memory.IsNull) return false;

        if (Layout == FieldLayout.Top)
        {
            AppDraw.Text(Memory.TextLabelStr);
            ImGui.Separator();
        }

        if (Layout != FieldLayout.None)
            ImGui.PushItemWidth(Layout == FieldLayout.Inline ? GuiTheme.FormItemInlineWidth : GuiTheme.FormItemWidth);

        var changed = OnDraw();

        if (Layout != FieldLayout.None) ImGui.PopItemWidth();

        if (changed) Set();
        return changed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected byte* GetLabel() => Layout == FieldLayout.Inline ? Memory.FullLabelStr : Memory.IdLabelStr;


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

}
/*
internal abstract unsafe class PropertyField<T> : PropertyField
    where T : unmanaged, IFieldValue
{
    private Func<T>? _getter;
    private Action<T>? _setter;

    public PropertyField(string name, int sizeInBytes, Func<T>? getter, Action<T>? setter)
        : base(name, T.Components * sizeof(float) + sizeInBytes)
    {
        _getter = getter;
        _setter = setter;

        if (getter is not null && setter is not null)
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
        *Memory.GetValue<T>() = getter();
    }

    protected override void Set()
    {
        if (!IsBound || _setter is not { } setter) return;
        setter.Invoke(*Memory.GetValue<T>());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref T Get()
    {
        if (IsBound && FetchStepper.Tick())
            *Memory.GetValue<T>() = _getter!();
        return ref *Memory.GetValue<T>();
    }
}*/