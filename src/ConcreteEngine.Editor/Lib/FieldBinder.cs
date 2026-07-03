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

internal sealed class BoundField<T>(string name, UiField widget, Func<T> getter, Action<T> setter)
    : BoundField(name, widget) where T : unmanaged, IFieldValue
{
    private FrameStepper _fetchStepper;

    public FieldGetDelay Delay
    {
        get;
        set
        {
            field = value;
            _fetchStepper.SetIntervalTicks((int)value, (int)value - 1);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Draw()
    {
        ref var value = ref Unsafe.As<byte, T>(ref Widget.GetRawValue());
        if (_fetchStepper.Tick()) value = getter();
        if (Widget.Draw()) setter(value);
    }

    public override void Refresh()
    {
        ref var value = ref Unsafe.As<byte, T>(ref Widget.GetRawValue());
        value = getter();
    }
}
