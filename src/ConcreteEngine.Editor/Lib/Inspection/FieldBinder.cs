using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Lib.Widgets;

namespace ConcreteEngine.Editor.Lib.Inspection;

internal abstract class BoundField
{
    public readonly string FieldName;
    public readonly UiField Widget;

    protected BoundField(string fieldName, UiField widget)
    {
        ArgumentException.ThrowIfNullOrEmpty(fieldName);
        ArgumentNullException.ThrowIfNull(widget);

        FieldName = fieldName;
        Widget = widget;
    }

    public abstract void Draw();
    public abstract void Refresh();
}

internal sealed class BoundField<T>(string fieldName, UiField widget, Func<T> getter, Action<T> setter)
    : BoundField(fieldName, widget) where T : unmanaged, IFieldValue
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
