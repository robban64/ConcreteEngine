using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Engine.Mesh;

/*
public sealed class GrassMeshChunk(MeshId meshId, VertexBufferId instanceVbo) : IDisposable
{
    private const int Capacity = TerrainChunk.ChunkQuads * TerrainChunk.ChunkQuads;

    public MeshId MeshId = meshId;
    public VertexBufferId InstanceVbo = instanceVbo;
    public BoundingBox Bounds;
    
    private NativeArray<GrassGpuInstance> _instanceData = NativeArray.Allocate<GrassGpuInstance>(Capacity, zeroed: true);

    public void Dispose() => _instanceData.Dispose();
}

internal sealed class GrassMeshGenerator(GfxContext gfx) : IDisposable
{
    public int GrassCount { get; set; }

    public MeshId MeshId { get; private set; }
    public IndexBufferId IboId { get; private set; }

    private void CreateMesh(int instanceCount)
    {
        InvalidOpThrower.ThrowIf(GrassCount <= 0);

        Span<Vector2> vertices = stackalloc Vector2[]
        {
            new(-0.5f, -0.5f), new(0.5f, -0.5f), new(-0.5f, 0.5f), new(0.5f, 0.5f)
        };

        var props = MeshDrawProperties.MakeInstance(
            DrawPrimitive.TriangleStrip,
            drawCount: 4,
            instances: instanceCount);

        var vertexBuilder = new VertexAttributeMaker();
        var particleBuilder = new VertexAttributeMaker();
        var gfxMeshes = gfx.Meshes;
        var meshId = gfxMeshes.CreateEmptyMesh(in props, 2, [
            vertexBuilder.Make<Vector2>(0), vertexBuilder.Make<Vector2>(1),
            particleBuilder.Make<Vector4>(2, 1), particleBuilder.Make<ColorRgba>(3, 1, VertexFormat.UByte, true)
        ]);
        gfxMeshes.CreateAttachVertexBuffer(meshId, vertices, CreateVboArgs.MakeDefault(0));
        gfxMeshes.CreateAttachVertexBuffer(meshId, ReadOnlySpan<GrassGpuInstance>.Empty,
            CreateVboArgs.MakeInstance(1, 2, instanceCount));

    }

    public void Dispose()
    {
        gfx.Disposer.EnqueueRemoval(MeshId);
    }
}*/