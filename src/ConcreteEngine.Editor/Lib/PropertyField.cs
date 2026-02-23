using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;

namespace ConcreteEngine.Editor.Lib;

public enum PropertyGetDelay
{
    Low = 2, Medium = 40, High = 160, VeryHigh = 1440
}

internal abstract class PropertyField(string name)
{
    internal static String8Utf8 DefaultInputLabel = new("##input");
    internal static String8Utf8 EmptyPlaceholder = new("Empty");

    private static int _idCounter = 1000;

    public readonly int Id = _idCounter++;
    public string Name { get; } = name;

    internal String16Utf8 NameUtf8 = new(name);

    protected FrameStepper Stepper = new((int)PropertyGetDelay.Low);

    public PropertyGetDelay Delay
    {
        get;
        set
        {
            value = field;
            Stepper.SetIntervalTicks((int)value);
        }
    } = PropertyGetDelay.Low;
}
