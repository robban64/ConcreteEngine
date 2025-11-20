#region

using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Batching;

public interface IRenderBatcher : IDisposable;

public abstract class RenderBatcher : IRenderBatcher
{
    protected readonly GfxContext Gfx;

    public MeshId MeshId { get; protected set; }
    public VertexBufferId[] VboIds { get; protected set; } =  Array.Empty<VertexBufferId>();
    public IndexBufferId IboId { get; protected set; }

    protected RenderBatcher(GfxContext gfx)
    {
        Gfx = gfx;
    }

    public abstract void BuildBatch();

    public abstract void Dispose();
}