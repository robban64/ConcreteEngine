namespace ConcreteEngine.Editor.Lib.Reflection;

public static class InspectorProvider
{
    private sealed class Entry(Type targetType, object provider, Func<object, object, object> selector)
    {
        private readonly Type TargetType = targetType;
        private readonly object Provider = provider;
        private readonly Func<object, object, object> Selector = selector;

        public object Get(object target)
        {
            return Selector(Provider, target);
        }
    }

    private static readonly Dictionary<Type, Entry> Providers = new(16);

    public static void Register(Type targetType, object provider, Func<object, object, object> selector)
    {
        if (!Providers.TryAdd(targetType, new Entry(targetType, provider, selector)))
            throw new ArgumentException($"Provider {targetType.Name} already registerd", nameof(targetType));
    }

    public static object InvokeSelect(Type type, object target)
    {
        if (!Providers.TryGetValue(type, out var entry))
            throw new ArgumentException($"Provider {type.Name} does not exists", nameof(target));

        return entry.Get(target);
    }
}