using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Widgets;

namespace ConcreteEngine.Editor.Lib;

internal static class Inspector
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ValidateName(string name, List<BoundField> fields)
    {
        foreach (var it in fields)
        {
            if (it.Name == name) throw new ArgumentException($"Name {name} is already registered", nameof(name));
        }
    }
}

internal static class Inspector<T> where T : class
{
    public static T? Target { get; private set; }
    private static readonly List<BoundField> Fields = new(4);

    public static event Action<T>? OnBind;
    public static event Action<T>? OnUnbind;

    public static ReadOnlySpan<BoundField> GetFields() => CollectionsMarshal.AsSpan(Fields);

    public static void Draw()
    {
        foreach (var it in GetFields()) it.Draw();
    }

    public static void Register<TValue>(string name, Func<TValue> getter, Action<TValue> setter,
        UiField el, FieldGetDelay delay = FieldGetDelay.Low)
        where TValue : unmanaged, IFieldValue
    {
        Inspector.ValidateName(name, Fields);
        var binder = new FieldBinder<TValue>(getter, setter);
        binder.Delay = delay;
        Fields.Add(new BoundField<TValue>(name, el, binder));
    }

    public static void Refresh()
    {
        if (Target is null) return;
        foreach (var field in Fields) field.Refresh();
    }

    public static void Bind(T target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (Target is not null)
            OnUnbind?.Invoke(Target);

        Target = target;
        OnBind?.Invoke(Target);

        Refresh();
    }

    public static void Unbind()
    {
        if (Target is null) return;
        OnUnbind?.Invoke(Target);
        Target = null;
    }
}