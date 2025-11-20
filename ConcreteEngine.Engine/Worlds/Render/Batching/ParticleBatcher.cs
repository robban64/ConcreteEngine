#region

using System.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Batching;

public sealed class ParticleBatcher : RenderBatcher
{
    public int ParticleCount { get; set; }
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
        
        var props = MeshDrawProperties.MakeInstance(drawCount: 4, instances: ParticleCount);
        var builder = Gfx.Meshes.StartUploadBuilder(in props);
        builder.UploadVertices(vertices, BufferUsage.DynamicDraw, BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        var attribBuilder = new VertexAttributeMaker<Vertex2D>();
        builder.AddAttribute(attribBuilder.Make<Vector2>(0, 0));
        builder.AddAttribute(attribBuilder.Make<Vector2>(1, 0));
        
        MeshId = Gfx.Meshes.FinishUploadBuilder(out _);
    }

    public override void BuildBatch()
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
    }
}