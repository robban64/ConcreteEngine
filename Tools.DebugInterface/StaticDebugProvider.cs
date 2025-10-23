namespace Tools.DebugInterface;

public static class StaticDebugProvider
{
    private static readonly Dictionary<string, Func<object?>> Targets = new(StringComparer.OrdinalIgnoreCase);

    public static void Bind(string name, object instance)
    {
        var wr = new WeakReference(instance);
        Targets[name] = () => wr.Target;
    }

    public static void Bind(string name, Func<object?> provider) => Targets[name] = provider;

    public static object? Resolve(string name) => Targets.TryGetValue(name, out var p) ? p() : null;
}