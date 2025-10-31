#region

using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Core.Worlds.Render.Batching;

public sealed class ParticleBatcher : RenderBatcher<TerrainBatchResult>
{
    private MeshId _meshId;
    private VertexBufferId _vertexBufferId;
    private IndexBufferId _indexBufferId;

    internal ParticleBatcher(GfxContext gfx) : base(gfx)
    {
    }

    private void GenerateMesh()
    {
        ReadOnlySpan<Vertex2D> vertices = stackalloc[]
        {
            new Vertex2D(-0.5f, -0.5f, 0f, 0f), new Vertex2D(0.5f, -0.5f, 1f, 0f),
            new Vertex2D(-0.5f, 0.5f, 0f, 1f), new Vertex2D(0.5f, 0.5f, 1f, 1f)
        };
    }

    public override TerrainBatchResult BuildBatch()
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
    }
}