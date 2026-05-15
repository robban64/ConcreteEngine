using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Engine.Mesh;

public readonly struct GrassBatcherResult(MeshId meshId, int drawCount, int instanceCount)
{
    public readonly int InstanceCount = instanceCount;
    public readonly MeshId MeshId = meshId;
    public readonly int DrawCount = drawCount;
}

internal sealed class GrassMeshGenerator(GfxContext gfx) : IDisposable
{
    public int GrassCount { get; set; }

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

    public void Dispose()
    {
        gfx.Disposer.EnqueueRemoval(MeshId);
    }
}