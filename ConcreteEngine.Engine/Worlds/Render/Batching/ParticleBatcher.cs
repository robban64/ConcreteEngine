#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Batching;


public sealed class ParticleBatcher : RenderBatcher
{
    public const int DefaultCapacity = 256;
    private ParticleInstanceData[] _particleData = Array.Empty<ParticleInstanceData>();
    
    private VertexBufferId _particleVbo = default;

    internal ParticleBatcher(GfxContext gfx) : base(gfx)
    {
    }

    internal int Capacity => _particleData.Length;
    internal Span<ParticleInstanceData> GetBufferSpan() => _particleData;

    internal void UploadGpuData()
    {
        Gfx.Buffers.UploadVertexBuffer<ParticleInstanceData>(_particleVbo, _particleData, 0);
    }

    private void GenerateMesh()
    {
        ReadOnlySpan<Vertex2D> vertices = stackalloc[]
        {
            new Vertex2D(-0.5f, -0.5f, 0f, 0f), new Vertex2D(0.5f, -0.5f, 1f, 0f),
            new Vertex2D(-0.5f, 0.5f, 0f, 1f), new Vertex2D(0.5f, 0.5f, 1f, 1f)
        };

        _particleData = new ParticleInstanceData[DefaultCapacity];

        var props = MeshDrawProperties.MakeInstance(DrawPrimitive.TriangleStrip, drawCount: 4,
            instances: DefaultCapacity);
        var builder = Gfx.Meshes.StartUploadBuilder(in props);

        builder.UploadVertices(vertices, BufferUsage.StaticDraw, BufferStorage.Static,
            BufferAccess.MapWrite);

        builder.UploadVertices<ParticleInstanceData>(
            _particleData,
            BufferUsage.DynamicDraw,
            BufferStorage.Dynamic,
            BufferAccess.MapWrite,
            divisor: 2);

        var vertexBuilder = new VertexAttributeMaker<Vertex2D>();
        builder.AddAttribute(vertexBuilder.Make<Vector2>(0, 0));
        builder.AddAttribute(vertexBuilder.Make<Vector2>(1, 0));

        var particleBuilder = new VertexAttributeMaker<ParticleInstanceData>();
        builder.AddAttribute(particleBuilder.Make<Vector4>(2, 1));
        builder.AddAttribute(particleBuilder.Make<Vector4>(3, 1));


        MeshId = Gfx.Meshes.FinishUploadBuilder(out _);
        var details = Gfx.Meshes.GetMeshDetails(MeshId, out var meta);
        VboIds = details.VboIds.ToArray();
        _particleVbo = VboIds[1];
    }

    public override void BuildBatch()
    {
        GenerateMesh();
    }

    public override void Dispose()
    {
        Gfx.Disposer.EnqueueRemoval(MeshId);
    }
}