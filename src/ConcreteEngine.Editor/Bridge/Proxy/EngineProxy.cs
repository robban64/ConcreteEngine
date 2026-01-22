namespace ConcreteEngine.Editor.Bridge.Proxy;

internal abstract class EngineProxy
{
    public bool Active { get; protected set; }

    public abstract void Deselect();
}