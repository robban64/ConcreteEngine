using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.Worlds.Render.Batching;

public readonly struct GrassBatcherResult(MeshId meshId, int drawCount, int instanceCount)
{
    public readonly int InstanceCount= instanceCount;
    public readonly MeshId MeshId = meshId;
    public readonly int DrawCount = drawCount;
}

internal sealed class GrassBatcher : RenderBatcher<GrassBatcherResult>
{
    public MeshId MeshId { get; private set; }
    public int GrassCount { get; set; }

    internal GrassBatcher(GfxContext gfx) : base(gfx)
    {
    }
    

    private void CreateMesh()
    {
        InvalidOpThrower.ThrowIf(GrassCount <= 0);
        
        Span<Vector2> vertices = stackalloc Vector2[]
        {
            new(-0.5f, -0.5f),
            new(0.5f, -0.5f),
            new(-0.5f, 0.5f),
            new(0.5f, 0.5f)
        };
        
        var props = MeshDrawProperties.MakeInstance(4, GrassCount);
        var builder = Gfx.Meshes.StartUploadBuilder(in props);
        //builder.UploadVertices();
        //builder.UploadVertices();
        
        
        
    }

    public override GrassBatcherResult BuildBatch()
    {
        CreateMesh();
        return default;
    }

    public override void Dispose()
    {
        Gfx.Disposer.EnqueueRemoval(MeshId);
    }
}