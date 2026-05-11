using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Engine.Mesh;

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

[StructLayout(LayoutKind.Sequential)]
internal struct ParticleInstanceData
{
    public Vector4 PositionSize;
    public Vector4 Color;
}

internal sealed class ParticleMeshGenerator : MeshGenerator
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
        if (!_particleData.IsNull)
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
        e.PreviousCount = len;
        return new ParticleMeshWriter(e.EmitterHandle, len, _particleData, e.GetParticleData(), _uploadGpuDel);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref ParticleMeshHandle GetHandle(int slot)
    {
        var idx = slot - 1;
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)idx, (uint)_handles.Length, nameof(slot));
        return ref _handles[idx];
    }

    internal void UploadGpuData(int slot, int count)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)count, (uint)_particleData.Length, nameof(count));
        var vboId = GetHandle(slot).VboInstanceId;

        if (!vboId.IsValid()) Throwers.InvalidHandle(vboId);

        Gfx.Buffers.UploadVertexBuffer(vboId, _particleData.AsSpan(0, count), 0);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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