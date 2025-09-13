using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public sealed class ParticleBatcher : RenderBatcher<TerrainBatchResult>
{
    private MeshId _meshId;
    private VertexBufferId _vertexBufferId;
    private IndexBufferId _indexBufferId;

    internal ParticleBatcher(IGraphicsRuntime graphics) : base(graphics)
    {
    }

    private void GenerateMesh()
    {
        ReadOnlySpan<Vertex2D> vertices = stackalloc []{
            new Vertex2D(-0.5f, -0.5f, 0f, 0f),
            new Vertex2D( 0.5f, -0.5f, 1f, 0f),
            new Vertex2D(-0.5f,  0.5f, 0f, 1f),
            new Vertex2D( 0.5f,  0.5f, 1f, 1f)
        };

        ReadOnlySpan<VertexAttributeDescriptor> pointers = stackalloc[]
        {
            VertexAttributeDescriptor.Make<Vertex2D>(nameof(Vertex2D.Position), VertexElementFormat.Float2),
            VertexAttributeDescriptor.Make<Vertex2D>(nameof(Vertex2D.TexCoords), VertexElementFormat.Float2)
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