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
    private RenderEntityMeta* _meta;
    private RenderSource* _sources;
    private DrawPolicy* _policies;

    private BoundingBox* _bounds;
    private Matrix4x4* _models;
    private Matrix3X4* _normals;
    
    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderEntityMeta GetMeta(RenderEntityId e) => ref _meta[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderSource GetSource(RenderEntityId e) => ref _sources[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawPolicy GetDrawPolicy(RenderEntityId e) => ref _policies[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoundingBox GetWorldBounds(RenderEntityId e) => ref _bounds[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix4x4 GetModelMatrix(RenderEntityId e) => ref _models[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix3X4 GetNormalMatrix(RenderEntityId e) => ref _normals[e.Index()];

    //
    
    //
    private void Allocate(int capacity)
    {
        if(_meta != null || Capacity != 0) Throwers.InvalidOperation("Already allocated");
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 32);
        _meta = NativeArray.AllocatePointer<RenderEntityMeta>(capacity);
        _sources = NativeArray.AllocatePointer<RenderSource>(capacity);
        _policies = NativeArray.AllocatePointer<DrawPolicy>(capacity);
        _bounds = NativeArray.AllocatePointer<BoundingBox>(capacity);
        _models = NativeArray.AllocatePointer<Matrix4x4>(capacity);
        _normals = NativeArray.AllocatePointer<Matrix3X4>(capacity);
        
        Capacity = capacity;
    }

    private void ClearEntityHeader(RenderEntityId e)
    {
        _meta[e.Index()] = default;
        _sources[e.Index()]= default;
        _policies[e.Index()] = default;
    }

    private void ClearEntitySpatial(RenderEntityId e)
    {
        _models[e.Index()] = Matrix4x4.Identity;
        _normals[e.Index()]= Matrix3X4.Identity;
        _bounds[e.Index()] = BoundingBox.One;
    }
    
    private void EnsureCapacity(int amount)
    {
        var required = Count + amount;
        if (Capacity >= required) return;

        var newSize = CapacityUtils.CapacityGrowthToFit(Capacity, required);
        Logger.Log(LogScope.Ecs, "RenderEcs resized", LogLevel.Warn);

        _meta = NativeArray.Resize(_meta, Capacity, newSize, 0, true);
        _sources = NativeArray.Resize(_sources, Capacity, newSize, 0, true);
        _policies = NativeArray.Resize(_policies, Capacity, newSize, 0, true);

        _bounds = NativeArray.Resize(_bounds, Capacity, newSize, 0, false);
        _models = NativeArray.Resize(_models, Capacity, newSize, 0, false);
        _normals = NativeArray.Resize(_normals, Capacity, newSize, 0, false);

        Capacity = newSize;
        foreach (var callback in _resizeCallbacks) callback(newSize);
    }


    public void Dispose()
    {
        NativeArray.DisposeArray(_meta, 0);
        NativeArray.DisposeArray(_sources, 0);
        NativeArray.DisposeArray(_policies, 0);
        NativeArray.DisposeArray(_bounds, 0);
        NativeArray.DisposeArray(_models, 0);
        NativeArray.DisposeArray(_normals, 0);
        _meta = null;
        _sources = null;
        _policies = null;
        _bounds = null;
        _models = null;
        _normals = null;
        
        Count = 0;
        Capacity = 0;
    }
}