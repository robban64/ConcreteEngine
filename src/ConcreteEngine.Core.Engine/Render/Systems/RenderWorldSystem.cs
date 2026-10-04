namespace ConcreteEngine.Core.Engine.Render.Systems;

public abstract class RenderWorldSystem : IDisposable
{
    public long FrameVersion { get; protected set; }

    public virtual void OnCoreResize(int newSize) {}
    public abstract void Dispose();
}