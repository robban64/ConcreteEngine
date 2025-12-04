#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.MeshGeneration.MeshData;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Engine.Worlds.MeshGeneration;

internal readonly ref struct ParticleMeshWriter(int slot, ParticleInstanceData[] particles, Action<int, int> uploadGpu)
{
    private readonly Action<int, int> _uploadGpuDel = uploadGpu;

    public readonly int Slot = slot;
    public readonly ParticleInstanceData[] Particles = particles;
    public void UploadGpuData(int particleCount) => _uploadGpuDel(Slot, particleCount);
}

public sealed class ParticleMeshGenerator : MeshGenerator
{
    private const int DefaultHandleCap = 16;
    public const int DefaultParticleCap = 1024 * 10;
    public const int MaxParticleInstanceCap = 16_384;
    public const int MaxMeshHandleCap = 128;
    
    private readonly struct ParticleMeshHandle(MeshId meshId,VertexBufferId vboInstanceId)
    {
        public readonly MeshId MeshId = meshId;
        public readonly VertexBufferId VboInstanceId = vboInstanceId;
    }

    private int _slot = 0;
    private int MakeSlot() => _slot++;

    private ParticleMeshHandle[] _handles;
    private ParticleInstanceData[] _particleData;

    private readonly Action<int, int> _uploadGpuDel;


    internal ParticleMeshGenerator(GfxContext gfx) : base(gfx)
    {
        _handles = new ParticleMeshHandle[DefaultHandleCap];
        _particleData = new ParticleInstanceData[DefaultParticleCap];

        _uploadGpuDel = UploadGpuData;
    }

    internal int Capacity => _particleData.Length;
    internal int HandleCapacity => _handles.Length;
    internal int NextSlot => _slot;

    internal ParticleMeshWriter GetWriteBuffer(int slot, int particleCount)
    {
        EnsureCapacity(particleCount);
        return new ParticleMeshWriter(slot, _particleData, _uploadGpuDel);
    }

    private ref ParticleMeshHandle GetHandle(int slot)
    {
        if ((uint)slot >= _handles.Length)
            throw new IndexOutOfRangeException();

        return ref _handles[slot];
    }

    internal void UploadGpuData(int slot, int particleCount)
    {
        if ((uint)particleCount > _particleData.Length)
            throw new IndexOutOfRangeException();

        ref readonly var handle = ref GetHandle(slot);
        var vboId = handle.VboInstanceId;

        if (!vboId.IsValid())
            throw new InvalidOperationException($"Invalid VboId {vboId}");

        Gfx.Buffers.UploadVertexBuffer(vboId, _particleData.AsSpan(0, particleCount), 0);
    }

    public override void Dispose()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_handles != null)
        {
            foreach (var handle in _handles)
                Gfx.Disposer.EnqueueRemoval(handle.MeshId);
        }

        _particleData = null!;
        _handles = null!;
        _slot = 0;
    }


    internal int CreateParticleMesh(int particleCapacity, out MeshId mesh)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(particleCapacity);
        EnsureCapacity(particleCapacity);

        ReadOnlySpan<Vertex2D> vertices = stackalloc[]
        {
            new Vertex2D(-0.5f, -0.5f, 0f, 0f), new Vertex2D(0.5f, -0.5f, 1f, 0f),
            new Vertex2D(-0.5f, 0.5f, 0f, 1f), new Vertex2D(0.5f, 0.5f, 1f, 1f)
        };

        var props = MeshDrawProperties.MakeInstance(DrawPrimitive.TriangleStrip, drawCount: 4,
            instances: particleCapacity);
        var builder = Gfx.Meshes.StartUploadBuilder(in props);

        builder.UploadVertices(vertices, BufferUsage.StaticDraw, BufferStorage.Static,
            BufferAccess.MapWrite);

        builder.UploadVerticesEmpty<ParticleInstanceData>(
            particleCapacity,
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


        mesh = Gfx.Meshes.FinishUploadBuilder(out _);
        var details = Gfx.Meshes.GetMeshDetails(mesh, out _);

        var slot = MakeSlot();
        _handles[slot] = new ParticleMeshHandle(mesh, details.VboIds[1]);
        return slot;
    }

    private void EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (capacity <= _particleData.Length) return;
        var newCap = Arrays.CapacityGrowthSafe(_particleData.Length, capacity, MaxParticleInstanceCap);
        _particleData = new ParticleInstanceData[newCap];
    }

    private void EnsureHandleCapacity(int delta)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(delta);
        var index = _slot + delta;
        if (index <= _handles.Length) return;
        var newCap = Arrays.CapacityGrowthSafe(_handles.Length, index, MaxMeshHandleCap);
        if (newCap > MaxMeshHandleCap) throw new InvalidOperationException("Maximum particle handle capacity exceeded");
        _handles = new ParticleMeshHandle[newCap];
    }
}