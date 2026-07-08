using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Lib.Widgets;

namespace ConcreteEngine.Editor.Lib.Inspection;

internal static class Inspector
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ValidateName(string name, List<BoundField> fields) { }

}

internal static class Inspector<T> where T : class
{
    public static T? Target { get; private set; }
    private static readonly List<BoundField> Fields = new(4);

    public static event Action<T>? OnBind;
    public static event Action<T>? OnUnbind;

    public static void Register(string fieldName, BoundField field)
    {
        Inspector.ValidateName(fieldName, Fields);
        Fields.Add(field);
    }


    public static void Register<TValue>(
        InputField input,
        Func<TValue> getter,
        Action<TValue> setter,
        FieldFetchDelay delay = FieldFetchDelay.Low)
        where TValue : unmanaged, INumberValue
    {
        Fields.Add(new BoundField<TValue>(input, getter, setter) { Delay = delay });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<BoundField> GetFields() => CollectionsMarshal.AsSpan(Fields);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Draw()
    {
        foreach (var it in GetFields()) it.Draw();
    }

    public static void Refresh()
    {
        if (Target is null) return;
        foreach (var field in GetFields()) field.Refresh();
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