using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Lib.Widgets;

namespace ConcreteEngine.Editor.Lib.Inspection;

internal abstract class BoundField(UiField widget)
{
    public readonly UiField Widget = widget;

    public abstract void Draw();
    public abstract void Refresh();
}

internal sealed class BoundField<T>(UiField widget, Func<T> getter, Action<T> setter) : BoundField(widget) 
    where T : unmanaged, INumberValue
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
