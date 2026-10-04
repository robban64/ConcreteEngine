namespace ConcreteEngine.Core.Engine.ECS.Render;

public abstract class RenderStore : IDisposable
{
    public virtual void OnCoreResize(int newSize){}
    public abstract void Dispose();
}
