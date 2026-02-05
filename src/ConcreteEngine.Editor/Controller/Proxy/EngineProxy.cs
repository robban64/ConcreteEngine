namespace ConcreteEngine.Editor.Controller.Proxy;

internal enum ProxyPropertyUpdateMode : byte
{
    SnapshotOnly,
    Continuous,
    Discrete
}

internal abstract class EngineProxy(Guid gId, int generation)
{
    public readonly Guid GId = gId;
    public readonly int Generation = generation;

    public bool Active { get; protected set; }

    public abstract void Free();
}