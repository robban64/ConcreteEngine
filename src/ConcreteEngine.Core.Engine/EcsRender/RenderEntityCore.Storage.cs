using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.EcsRender;

public sealed unsafe partial class RenderEntityCore
{
    private ushort* _generations;
    private byte* _visibility;
    private DrawPolicy* _policies;
    private RenderSource* _sources;

    private BoundingAxisBox* _bounds;
    private TransformUniform* _transforms;

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetVisibilityMask(RenderEntity e) => ref _visibility[e.Index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderSource GetSource(RenderEntity e) => ref _sources[e.Index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawPolicy GetDrawPolicy(RenderEntity e) => ref _policies[e.Index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoundingAxisBox GetWorldBounds(RenderEntity e) => ref _bounds[e.Index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix4x4 GetModelMatrix(RenderEntity e) => ref _transforms[e.Index].Model;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix3X4 GetNormalMatrix(RenderEntity e) => ref _transforms[e.Index].Normal;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TransformUniform GetTransformData(RenderEntity e) => ref _transforms[e.Index];


    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<byte> GetVisibilityView() => new(_visibility, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<RenderSource> GetSourceView() => new(_sources, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<DrawPolicy> GetDrawPolicyView() => new(_policies, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<BoundingAxisBox> GetWorldBoundView() => new(_bounds, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<TransformUniform> GetTransformView() => new(_transforms, Count);
    //

    //
    private void Allocate(int capacity)
    {
        if (_policies != null || Capacity != 0) Throwers.InvalidOperation("Already allocated");
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 32);
        _generations = NativeArray.AllocatePointer<ushort>(capacity);
        _visibility = NativeArray.AllocatePointer<byte>(capacity);
        _policies = NativeArray.AllocatePointer<DrawPolicy>(capacity);
        _sources = NativeArray.AllocatePointer<RenderSource>(capacity);
        _bounds = NativeArray.AllocatePointer<BoundingAxisBox>(capacity, false);
        _transforms = NativeArray.AllocatePointer<TransformUniform>(capacity, false);

        Capacity = capacity;
    }

    private void ClearEntityHeader(RenderEntity e)
    {
        _generations[e.Index] = 0;
        _visibility[e.Index] = 0;
        _sources[e.Index] = default;
        _policies[e.Index] = default;
    }

    private void ClearEntitySpatial(RenderEntity e)
    {
        _transforms[e.Index].Model = Matrix4x4.Identity;
        _transforms[e.Index].Normal = Matrix3X4.Identity;
        _bounds[e.Index] = default;
    }

    private void EnsureCapacity(int amount)
    {
        var required = Count + amount;
        if (Capacity >= required) return;

        var newSize = CapacityUtils.CapacityGrowthToFit(Capacity, required);
        Logger.Log(LogScope.Ecs, "RenderEcs resized", LogLevel.Warn);

        _generations = NativeArray.ReAlloc(_generations, Capacity, newSize, 0, true);
        _visibility = NativeArray.ReAlloc(_visibility, Capacity, newSize, 0, true);
        _sources = NativeArray.ReAlloc(_sources, Capacity, newSize, 0, true);
        _policies = NativeArray.ReAlloc(_policies, Capacity, newSize, 0, true);

        _bounds = NativeArray.ReAlloc(_bounds, Capacity, newSize, 0, false);
        _transforms = NativeArray.ReAlloc(_transforms, Capacity, newSize, 0, false);

        Capacity = newSize;
        RenderEcs.OnResize(newSize);
    }


    public void Dispose()
    {
        NativeArray.DisposeArray(_generations, Capacity * Unsafe.SizeOf<RenderSource>(), 0);
        NativeArray.DisposeArray(_visibility, Capacity, 0);
        NativeArray.DisposeArray(_sources, Capacity * Unsafe.SizeOf<RenderSource>(), 0);
        NativeArray.DisposeArray(_policies, Capacity * Unsafe.SizeOf<DrawPolicy>(), 0);
        NativeArray.DisposeArray(_bounds, Capacity * Unsafe.SizeOf<BoundingBox>(), 0);
        NativeArray.DisposeArray(_transforms, Capacity * Unsafe.SizeOf<TransformUniform>(), 0);
        _generations = null;
        _visibility = null;
        _sources = null;
        _policies = null;
        _bounds = null;
        _transforms = null;


        Count = 0;
        Capacity = 0;
    }
}