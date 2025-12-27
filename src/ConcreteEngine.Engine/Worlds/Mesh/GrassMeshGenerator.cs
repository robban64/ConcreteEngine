using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Worlds.Mesh;

public readonly struct GrassBatcherResult(MeshId meshId, int drawCount, int instanceCount)
{
    public readonly int InstanceCount = instanceCount;
    public readonly MeshId MeshId = meshId;
    public readonly int DrawCount = drawCount;
}

internal sealed class GrassMeshGenerator : MeshGenerator
{
    public int GrassCount { get; set; }

    internal GrassMeshGenerator(GfxContext gfx) : base(gfx)
    {
    }

    public MeshId MeshId { get; private set; }
    public IndexBufferId IboId { get; private set; }

    private void CreateMesh()
    {
        InvalidOpThrower.ThrowIf(GrassCount <= 0);

        Span<Vector2> vertices = stackalloc Vector2[]
        {
            new(-0.5f, -0.5f), new(0.5f, -0.5f), new(-0.5f, 0.5f), new(0.5f, 0.5f)
        };

        //builder.UploadVertices();
        //builder.UploadVertices();
    }

    public override void Dispose()
    {
        Gfx.Disposer.EnqueueRemoval(MeshId);
    }
}