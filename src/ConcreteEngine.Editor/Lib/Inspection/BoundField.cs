namespace ConcreteEngine.Editor.Lib.Inspection;
/*
internal abstract class BoundField(InputField field)
{
    public readonly InputField Field = field;

    public abstract void Draw();
    public abstract void Refresh();
}

internal sealed class BoundField<T>(InputField field, Func<T> getter, Action<T> setter) : BoundField(field) 
    where T : unmanaged, INumberValue
{
    private FrameStepper _fetchStepper;

    public FieldFetchDelay Delay
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
        ref var value = ref Unsafe.As<byte, T>(ref Field.GetRawValue());
        if (_fetchStepper.Tick()) value = getter();
        if (Field.Draw()) setter(value);
    }

    public override void Refresh()
    {
        ref var value = ref Unsafe.As<byte, T>(ref Field.GetRawValue());
        value = getter();
    }
}
*/