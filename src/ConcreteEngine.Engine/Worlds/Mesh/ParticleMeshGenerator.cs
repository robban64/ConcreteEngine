using System.Numerics;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Worlds.Mesh.Data;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Worlds.Mesh;

internal readonly ref struct ParticleMeshWriter(
    int slot,
    int particleCount,
    NativeArray<ParticleInstanceData> gpuParticles,
    ReadOnlySpan<ParticleStateData> particles,
    Action<int, int> uploadGpu)
{
    public readonly UnsafeSpan<ParticleInstanceData> GpuParticleSpan = new(ref gpuParticles[0], particleCount);
    public readonly ReadOnlySpan<ParticleStateData> ParticleSpan = particles;

    public readonly int Slot = slot;
    public readonly int ParticleCount = particleCount;

    public void UploadGpuData() => uploadGpu(Slot, ParticleCount);
}

public sealed class ParticleMeshGenerator : MeshGenerator
{
    private const int DefaultHandleCap = 16;
    public const int DefaultParticleCap = 1024 * 10;
    public const int MaxParticleInstanceCap = 16_384;
    public const int MaxMeshHandleCap = 128;

    private readonly struct ParticleMeshHandle(MeshId meshId, VertexBufferId vboInstanceId)
    {
        public readonly MeshId MeshId = meshId;
        public readonly VertexBufferId VboInstanceId = vboInstanceId;
    }

    private int _count;

    private ParticleMeshHandle[] _handles;
    private NativeArray<ParticleInstanceData> _particleData;

    private readonly Action<int, int> _uploadGpuDel;


    internal ParticleMeshGenerator(GfxContext gfx) : base(gfx)
    {
        if(!_particleData.IsNull) 
            throw new InvalidOperationException($"{nameof(ParticleMeshGenerator)} is already initialized");
        
        _handles = new ParticleMeshHandle[DefaultHandleCap];
        _particleData = NativeArray.Allocate<ParticleInstanceData>(DefaultParticleCap);

        _uploadGpuDel = UploadGpuData;
    }

    internal int Capacity => _particleData.Length;
    internal int HandleCapacity => _handles.Length;
    internal int NextCount => _count;

    internal ParticleMeshWriter GetWriteBuffer(ParticleEmitter e)
    {
        var len = e.ParticleCount;
        EnsureCapacity(len);
        return new ParticleMeshWriter(e.EmitterHandle, len, _particleData, e.GetParticleData(), _uploadGpuDel);
    }

    private ref ParticleMeshHandle GetHandle(int slot)
    {
        var idx = slot - 1;
        if ((uint)idx >= _handles.Length)
            throw new IndexOutOfRangeException();

        return ref _handles[idx];
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

        _particleData.Dispose();
        _handles = null!;
        _count = 0;
    }


    internal int CreateParticleMesh(int particleCapacity, out MeshId meshId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(particleCapacity);
        EnsureCapacity(particleCapacity);
        var gfxMeshes = Gfx.Meshes;

        ReadOnlySpan<Vertex2D> vertices = stackalloc[]
        {
            new Vertex2D(-0.5f, -0.5f, 0f, 0f), new Vertex2D(0.5f, -0.5f, 1f, 0f),
            new Vertex2D(-0.5f, 0.5f, 0f, 1f), new Vertex2D(0.5f, 0.5f, 1f, 1f)
        };

        var props = MeshDrawProperties.MakeInstance(DrawPrimitive.TriangleStrip, drawCount: 4,
            instances: particleCapacity);

        var vertexBuilder = new VertexAttributeMaker();
        var particleBuilder = new VertexAttributeMaker();

        meshId = Gfx.Meshes.CreateEmptyMesh(in props, 2, [
            vertexBuilder.Make<Vector2>(0), vertexBuilder.Make<Vector2>(1),
            particleBuilder.Make<Vector4>(2, 1), particleBuilder.Make<Vector4>(3, 1)
        ]);
        gfxMeshes.CreateAttachVertexBuffer(meshId, vertices, CreateVboArgs.MakeDefault(0));
        gfxMeshes.CreateAttachVertexBuffer(meshId, ReadOnlySpan<ParticleInstanceData>.Empty,
            CreateVboArgs.MakeInstance(1, 2, particleCapacity));

        var details = Gfx.Meshes.GetMeshDetails(meshId, out _);

        var index = _count++;
        _handles[index] = new ParticleMeshHandle(meshId, details.VboIds[1]);
        return index;
    }

    private void EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (capacity <= _particleData.Length) return;
        var newCap = Arrays.CapacityGrowthSafe(_particleData.Length, capacity, MaxParticleInstanceCap);
        _particleData.Resize(newCap, true);
    }

    private void EnsureHandleCapacity(int delta)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(delta);
        var index = _count + delta;
        if (index <= _handles.Length) return;
        var newCap = Arrays.CapacityGrowthSafe(_handles.Length, index, MaxMeshHandleCap);
        if (newCap > MaxMeshHandleCap) throw new InvalidOperationException("Maximum particle handle capacity exceeded");
        _handles = new ParticleMeshHandle[newCap];
    }
}