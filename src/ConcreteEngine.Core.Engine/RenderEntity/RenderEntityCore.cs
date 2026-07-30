using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public unsafe struct RenderCoreStorage
{
    private RenderEntityMeta* _meta;
    private RenderSource* _sources;
    private DrawPolicy* _policies;

    private BoundingBox* _bounds;
    private Matrix4x4* _models;
    private Matrix3X4* _normals;
   
   public void Allocate(int initialCapacity)
   {
       ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
       _meta = NativeArray.AllocatePointer<RenderEntityMeta>(initialCapacity);
       _sources = NativeArray.AllocatePointer<RenderSource>(initialCapacity);
       _policies = NativeArray.AllocatePointer<DrawPolicy>(initialCapacity);
       _bounds = NativeArray.AllocatePointer<BoundingBox>(initialCapacity);
       _models = NativeArray.AllocatePointer<Matrix4x4>(initialCapacity);
       _normals = NativeArray.AllocatePointer<Matrix3X4>(initialCapacity);
   }


   public void Resize(int capacity, int newSize)
   {
       _meta = NativeArray.Resize(_meta, capacity, newSize, 0, true);
       _sources = NativeArray.Resize(_sources, capacity, newSize, 0, true);
       _policies = NativeArray.Resize(_policies, capacity, newSize, 0, true);
       _bounds = NativeArray.Resize(_bounds, capacity, newSize, 0, false);
       _models = NativeArray.Resize(_models, capacity, newSize, 0, false);
       _normals = NativeArray.Resize(_normals, capacity, newSize, 0, false);
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
   }


}


public sealed unsafe partial class RenderEntityCore : IDisposable
{
    public int Count { get; private set; }
    public int Capacity { get; private set; }

    private readonly Stack<int> _free = [];
    private readonly List<Action<int>> _resizeCallbacks = [];

    internal RenderEntityCore(int initialCapacity)
    {
        Allocate(initialCapacity);
    }

    public int ActiveCount => Count - _free.Count;

    public void AddResizeCallback(Action<int> callback) => _resizeCallbacks.Add(callback);
    public void RemoveResizeCallback(Action<int> callback) => _resizeCallbacks.Remove(callback);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(RenderEntityId e)
    {
        var index = e.Index();
        return (uint)index < (uint)Capacity && _meta[index].Alive;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(RenderEntityId e)
    {
        var index = e.Index();
        return (uint)index < (uint)Capacity && _meta[index].IsVisible();
    }

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityVisibility ToggleVisibility(RenderEntityId entity, EntityVisibility flag)
    {
        if (!IsAlive(entity)) Throwers.InvalidOperation(nameof(entity));
        return _meta[entity.Index()].Visibility = flag;
    }

    public RenderEntityId AddEntity(RenderSource source, DrawPolicy policy)
    {
        ValidateSource(source);

        var index = SlotHelper.NextSlot(_free, Count);
        if (index < 0)
        {
            if (Count >= Capacity) EnsureCapacity(1);
            index = Count++;
        }
        
        var entity = new RenderEntityId(index + 1);

        if (_meta[index].Alive) Throwers.InvalidOperation("Entity already exists");
        _meta[index].Alive = true;
        _meta[index].Visibility = EntityVisibility.Visible;
        _sources[index] = source;
        _policies[index] = policy;
        ClearEntitySpatial(entity);
        
        return entity;
    }

    public void Remove(RenderEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(entity.Id, Count, nameof(entity));

        if (!IsAlive(entity)) Throwers.InvalidArgument(nameof(entity));

        ClearEntityHeader(entity);
        ClearEntitySpatial(entity);

        Count = SlotHelper.FreeSlot(_free, entity.Index(), Count);
    }


    [StackTraceHidden]
    private static void ValidateSource(RenderSource source)
    {
        if (source.Kind == EntitySourceKind.Particle) return;
        ArgumentOutOfRangeException.ThrowIfZero(source.Mesh.Id, nameof(source.Mesh));
        ArgumentOutOfRangeException.ThrowIfZero(source.Material.Value, nameof(source.Material));
        ArgumentOutOfRangeException.ThrowIfEqual((int)source.Kind, (int)EntitySourceKind.Unknown, nameof(source.Kind));
    }
}
