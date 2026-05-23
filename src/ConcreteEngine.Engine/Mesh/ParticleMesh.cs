using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Types;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Engine.Mesh;

[StructLayout(LayoutKind.Sequential)]
internal struct ParticleGpuInstance
{
    public Vector4 PositionSize;
    public ColorRgba Color;
}

internal readonly struct ParticleMeshHandle(MeshId meshId, VertexBufferId vboInstanceId)
{
    public readonly MeshId MeshId = meshId;
    public readonly VertexBufferId VboInstanceId = vboInstanceId;
}

internal sealed class ParticleMesh : IDisposable
{
    private const int DefaultHandleCap = 16;

    public const int DefaultParticleCap = 1024 * 10;
    public const int MaxParticleInstanceCap = 16_384;
    public const int MaxMeshHandleCap = 128;

    public int Count { get; private set; }

    private ParticleMeshHandle[] _handles;
    private NativeArray<ParticleGpuInstance> _particleData;

    private readonly GfxContext _gfx;

    internal ParticleMesh(GfxContext gfx)
    {
        if (!_particleData.IsNull)
            throw new InvalidOperationException($"{nameof(ParticleMesh)} is already initialized");

        _handles = new ParticleMeshHandle[DefaultHandleCap];
        _particleData = NativeArray.Allocate<ParticleGpuInstance>(DefaultParticleCap);

        _gfx = gfx;
    }

    public int Capacity => _particleData.Length;
    public int HandleCapacity => _handles.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<ParticleGpuInstance> GetBufferView(int count)
    {
        if (_particleData.IsNull) Throwers.NullPointer(nameof(_particleData));

        EnsureCapacity(count);
        return _particleData.Slice(0, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly ParticleMeshHandle GetHandle(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)slot, (uint)_handles.Length, nameof(slot));
        return ref _handles[slot];
    }

    public void UploadGpuData(int slot, int count)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)count, (uint)_particleData.Length, nameof(count));
        var vboId = GetHandle(slot).VboInstanceId;

        if (!vboId.IsValid()) Throwers.InvalidHandle(vboId);

        _gfx.Buffers.UploadVertexBuffer(vboId, _particleData.AsSpan(0, count), 0);
    }


    public int CreateParticleMesh(int particleCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(particleCapacity);
        EnsureCapacity(particleCapacity);
        EnsureHandleCapacity(1);

        var gfxMeshes = _gfx.Meshes;

        ReadOnlySpan<Vertex2D> vertices = stackalloc[]
        {
            new Vertex2D(-0.5f, -0.5f, 0f, 0f), new Vertex2D(0.5f, -0.5f, 1f, 0f),
            new Vertex2D(-0.5f, 0.5f, 0f, 1f), new Vertex2D(0.5f, 0.5f, 1f, 1f)
        };

        var props = MeshDrawProperties.MakeInstance(
            DrawPrimitive.TriangleStrip,
            drawCount: 4,
            instances: particleCapacity);

        var vertexBuilder = new VertexAttributeMaker();
        var particleBuilder = new VertexAttributeMaker();

        var meshId = gfxMeshes.CreateEmptyMesh(in props, 2, [
            vertexBuilder.Make<Vector2>(0), vertexBuilder.Make<Vector2>(1),
            particleBuilder.Make<Vector4>(2, 1), particleBuilder.Make<ColorRgba>(3, 1, VertexFormat.UByte, true)
        ]);
        gfxMeshes.CreateAttachVertexBuffer(meshId, vertices, CreateVboArgs.MakeDefault(0));
        gfxMeshes.CreateAttachVertexBuffer(meshId, ReadOnlySpan<ParticleGpuInstance>.Empty,
            CreateVboArgs.MakeInstance(1, 2, particleCapacity));

        var details = gfxMeshes.GetMeshDetails(meshId, out _);

        var index = Count++;
        _handles[index] = new ParticleMeshHandle(meshId, details.VboIds[1]);
        return index;
    }


    public void Dispose()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_handles != null)
        {
            foreach (var handle in _handles)
                _gfx.Disposer.EnqueueRemoval(handle.MeshId);
        }

        _particleData.Dispose();
        _handles = null!;
        Count = 0;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (capacity <= _particleData.Length) return;
        var newCap = CapacityUtils.CapacityGrowthToFit(_particleData.Length, capacity);
        _particleData.Resize(newCap, true);
        Logger.LogString(LogScope.Engine, $"{nameof(_particleData)} resize");
    }

    private void EnsureHandleCapacity(int delta)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(delta);
        var len = Count + delta;
        if (len <= _handles.Length) return;
        var newCap = CapacityUtils.CapacityGrowthToFit(_handles.Length, len);

        if (newCap >= MaxMeshHandleCap)
            throw new InvalidOperationException("Maximum particle handle capacity exceeded");
        Array.Resize(ref _handles, newCap);
        Logger.LogString(LogScope.Engine, $"{nameof(_handles)} resize");
    }
}