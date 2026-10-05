namespace ConcreteEngine.Core.Engine.Render;

public abstract class RenderStore : IDisposable
{
    internal virtual void OnDenseResized(int newSize){}
    public abstract void Dispose();
}
