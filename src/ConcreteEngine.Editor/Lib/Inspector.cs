namespace ConcreteEngine.Editor.Lib;

internal abstract class Inspector { }

internal abstract class Inspector<TSelf> : Inspector where TSelf : Inspector<TSelf>
{
    public static TSelf Instance { get; private set; } = null!;

    protected Inspector()
    {
        if (Instance != null) throw new InvalidOperationException($"{typeof(TSelf).Name} already initialized.");
        Instance = (TSelf)this;
    }
}