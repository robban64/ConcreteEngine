namespace ConcreteEngine.Core.Engine.Render;

public abstract class RenderStore : IDisposable
{
    internal virtual void OnCoreResize(int newSize){}
    public abstract void Dispose();
}
