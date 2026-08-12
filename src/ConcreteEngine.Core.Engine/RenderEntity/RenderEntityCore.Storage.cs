using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public sealed unsafe partial class RenderEntityCore
{
    private RenderSource* _sources;
    private DrawPolicy* _policies;

    private BoundingAxisBox* _bounds;
    private Matrix4x4* _models;
    private Matrix3X4* _normals;

    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderSource GetSource(RenderEntityId e) => ref _sources[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawPolicy GetDrawPolicy(RenderEntityId e) => ref _policies[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoundingAxisBox GetWorldBounds(RenderEntityId e) => ref _bounds[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix4x4 GetModelMatrix(RenderEntityId e) => ref _models[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix3X4 GetNormalMatrix(RenderEntityId e) => ref _normals[e.Index()];

    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<RenderSource> GetSourceView() => new(_sources, 0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<DrawPolicy> GetDrawPolicyView() => new(_policies, 0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<BoundingAxisBox> GetWorldBoundView() => new(_bounds, 0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<Matrix4x4> GetModelMatrixView() => new(_models, 0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<Matrix3X4> GetNormalMatrixView() => new(_normals, 0, Count);
    //
    

    //
    private void Allocate(int capacity)
    {
        if (_sources != null || Capacity != 0) Throwers.InvalidOperation("Already allocated");
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 32);
        _sources = NativeArray.AllocatePointer<RenderSource>(capacity);
        _policies = NativeArray.AllocatePointer<DrawPolicy>(capacity);
        _bounds = NativeArray.AllocatePointer<BoundingAxisBox>(capacity, false);
        _models = NativeArray.AllocatePointer<Matrix4x4>(capacity, false);
        _normals = NativeArray.AllocatePointer<Matrix3X4>(capacity, false);

        Capacity = capacity;
    }

    private void ClearEntityHeader(RenderEntityId e)
    {
        _sources[e.Index()] = default;
        _policies[e.Index()] = default;
    }

    private void ClearEntitySpatial(RenderEntityId e)
    {
        _models[e.Index()] = Matrix4x4.Identity;
        _normals[e.Index()] = Matrix3X4.Identity;
        _bounds[e.Index()] = default;
    }

    private void EnsureCapacity(int amount)
    {
        var required = Count + amount;
        if (Capacity >= required) return;

        var newSize = CapacityUtils.CapacityGrowthToFit(Capacity, required);
        Logger.Log(LogScope.Ecs, "RenderEcs resized", LogLevel.Warn);

        _sources = NativeArray.ReAlloc(_sources, Capacity, newSize, 0, true);
        _policies = NativeArray.ReAlloc(_policies, Capacity, newSize, 0, true);

        _bounds = NativeArray.ReAlloc(_bounds, Capacity, newSize, 0, false);
        _models = NativeArray.ReAlloc(_models, Capacity, newSize, 0, false);
        _normals = NativeArray.ReAlloc(_normals, Capacity, newSize, 0, false);

        Capacity = newSize;
        RenderEcs.OnResize(newSize);
    }


    public void Dispose()
    {
        NativeArray.DisposeArray(_sources, Capacity * Unsafe.SizeOf<RenderSource>(), 0);
        NativeArray.DisposeArray(_policies, Capacity * Unsafe.SizeOf<DrawPolicy>(), 0);
        NativeArray.DisposeArray(_bounds, Capacity * Unsafe.SizeOf<BoundingBox>(), 0);
        NativeArray.DisposeArray(_models, Capacity * Unsafe.SizeOf<Matrix4x4>(), 0);
        NativeArray.DisposeArray(_normals, Capacity * Unsafe.SizeOf<Matrix3X4>(), 0);
        _sources = null;
        _policies = null;
        _bounds = null;
        _models = null;
        _normals = null;

        Count = 0;
        Capacity = 0;
    }
}