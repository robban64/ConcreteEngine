using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Widgets;

namespace ConcreteEngine.Editor.Lib;
/*
internal interface IFieldBinding
{
    void SetFetchInterval(int intervalTicks, int ticks = 0);
    void Unbind();
}

internal static class FieldBinding<T>
{
    public static T Inspector;
    public static FieldA Field;

    public void Draw()
    {
        Field.Draw();
    }

}

internal abstract class FieldA
{
    public abstract UiElement Widget { get; set; }
    public abstract void Draw();
}


internal sealed class Field<T>(NumberInput<T> widget, FieldBinder<T> binding) : FieldA
    where T : unmanaged, IFieldValue
{
    public readonly FieldBinder<T> Binding = binding;
    public NumberInput<T> WidgetA { get; set; } = widget;
    public override UiElement Widget { get; set; } 

    public override void Draw()
    {
        Binding.Get(ref WidgetA.Value);

        if (Widget.Draw())
            Binding.Set(WidgetA.Value);
    }
}

internal sealed class FieldBinder<T> : IFieldBinding where T : unmanaged, IFieldValue
{
    private Func<T>? _getter;
    private Action<T>? _setter;
    private FrameStepper _fetchStepper;

    public bool IsBound => _getter != null && _setter != null;

    public void SetFetchInterval(int intervalTicks, int ticks = 0) =>
        _fetchStepper.SetIntervalTicks(intervalTicks, ticks);

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

    public void Refresh(scoped ref T value)
    {
        if (_getter is not { } getter) return;
        value = getter();
    }

    public void Set(T value)
    {
        if (_setter is not { } setter) return;
        setter.Invoke(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Get(scoped ref T value)
    {
        if (_getter is { } getter && _fetchStepper.Tick())
            value = getter();
    }
}*/