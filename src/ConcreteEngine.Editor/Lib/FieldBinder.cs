using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Widgets;

namespace ConcreteEngine.Editor.Lib;

internal abstract class BoundField
{
    public readonly string Name;
    public readonly UiField Widget;

    public BoundField(string name, UiField widget)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(widget);

        Name = name;
        Widget = widget;
    }

    public abstract void Draw();
    public abstract void Refresh();
}

internal sealed class BoundField<T>(string name, UiField widget, FieldBinder<T> binder)
    : BoundField(name, widget) where T : unmanaged, IFieldValue
{
    public readonly FieldBinder<T> Binder = binder;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Draw()
    {
        ref var value = ref Unsafe.As<byte, T>(ref Widget.GetRawValue());
        Binder.Get(ref value);
        if (Widget.Draw())
            Binder.Set(value);
    }

    public override void Refresh()
    {
        ref var value = ref Unsafe.As<byte, T>(ref Widget.GetRawValue());
        Binder.Refresh(ref value);
    }
}

internal sealed class FieldBinder<T> where T : unmanaged, IFieldValue
{
    private FrameStepper _fetchStepper;

    private readonly Func<T> _getter;
    private readonly Action<T> _setter;


    public FieldBinder(Func<T> getter, Action<T> setter)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        _getter = getter;
        _setter = setter;
        Delay = FieldGetDelay.Low;
    }

    public FieldGetDelay Delay
    {
        get;
        set
        {
            field = value;
            _fetchStepper.SetIntervalTicks((int)value, (int)value - 1);
        }
    }

    public void Refresh(scoped ref T value) => value = _getter();

    public void Set(T value) => _setter(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Get(scoped ref T value)
    {
        if (_fetchStepper.Tick()) value = _getter();
    }
}