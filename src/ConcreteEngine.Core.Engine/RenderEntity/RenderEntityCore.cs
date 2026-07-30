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

public sealed unsafe class RenderEntityCore
{
    public int Count { get; private set; }
    public int Capacity { get; private set; }

    private RenderEntityMeta* _meta;
    private RenderSource* _sources;
    private DrawPolicy* _policies;

    private BoundingBox* _bounds;
    private Matrix4x4* _models;
    private Matrix3X4* _normals;

    private readonly Stack<int> _free = [];
    private readonly List<Action<int>> _resizeCallbacks = [];

    internal RenderEntityCore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
        _meta = NativeArray.AllocatePointer<RenderEntityMeta>(initialCapacity);
        _sources = NativeArray.AllocatePointer<RenderSource>(initialCapacity);
        _policies = NativeArray.AllocatePointer<DrawPolicy>(initialCapacity);
        _bounds = NativeArray.AllocatePointer<BoundingBox>(initialCapacity);
        _models = NativeArray.AllocatePointer<Matrix4x4>(initialCapacity);
        _normals = NativeArray.AllocatePointer<Matrix3X4>(initialCapacity);
        Capacity = initialCapacity;
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityVisibility ToggleVisibility(RenderEntityId entity, EntityVisibility flag, bool isVisible)
    {
        if (!IsAlive(entity)) Throwers.InvalidOperation(nameof(entity));
        return _meta[entity.Index()].ToggleVisibility(flag, isVisible);
    }

    public RenderEntityId AddEntity(RenderSource source, DrawPolicy policy)
    {
        ValidateSource(source);

        var entity = AllocateNewEntity();
        var index = entity.Index();

        _sources[index] = source;
        _policies[index] = policy;

        _models[index] = Matrix4x4.Identity;
        _normals[index]= Matrix3X4.Identity;
        _bounds[index] = BoundingBox.One;
        return entity;
    }


    private RenderEntityId AllocateNewEntity()
    {
        var index = SlotHelper.NextSlot(_free, Count);
        if (index < 0)
        {
            if (Count >= Capacity) EnsureCapacity(1);
            index = Count++;
        }

        ref var entity = ref _meta[index];
        if (entity.Alive) Throwers.InvalidOperation("Entity already exists");
        entity.Alive = true;
        entity.Visibility = EntityVisibility.Visible;
        return new RenderEntityId(index + 1);
    }

    public void Remove(RenderEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(entity.Id, Count, nameof(entity));

        var index = entity.Index();
        if (!_meta[index].Alive) throw new InvalidOperationException();

        _meta[index] = default;
        _sources[index] = default;
        _policies[index] = default;
        _models[index] = Matrix4x4.Identity;
        _normals[index]= Matrix3X4.Identity;
        _bounds[index] = BoundingBox.One;

        Count = SlotHelper.FreeSlot(_free, index, Count);
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BoundsQueryEnumerator BoundsQuery() => new(_meta, _bounds, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibleCoreEnumerator VisibilityQuery() => new(this, _meta, Count);

    [StackTraceHidden]
    private static void ValidateSource(RenderSource source)
    {
        if (source.Kind == EntitySourceKind.Particle) return;
        ArgumentOutOfRangeException.ThrowIfZero(source.Mesh.Id, nameof(source.Mesh));
        ArgumentOutOfRangeException.ThrowIfZero(source.Material.Value, nameof(source.Material));
        ArgumentOutOfRangeException.ThrowIfEqual((int)source.Kind, (int)EntitySourceKind.Unknown, nameof(source.Kind));
    }
}

public unsafe ref struct BoundsQueryEnumerator
{
    private int _entity;
    private RenderEntityMeta* _entities;
    private BoundingBox* _boxes;
    private readonly RenderEntityMeta* _end;

    public BoundsQueryEnumerator(RenderEntityMeta* entities, BoundingBox* boxes, int length)
    {
        _entity = 0;
        _entities = entities - 1;
        _boxes = boxes - 1;
        _end = entities + length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        while (++_entities < _end)
        {
            ++_boxes;
            ++_entity;
            if (_entities->Alive) return true;
        }

        return false;
    }

    public readonly Item Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(new RenderEntityId(_entity), ref *_entities, ref *_boxes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BoundsQueryEnumerator GetEnumerator() => this;

    public readonly ref struct Item(RenderEntityId entity, ref RenderEntityMeta meta, ref BoundingBox bounds)
    {
        public readonly RenderEntityId Entity = entity;
        public readonly ref RenderEntityMeta Meta = ref meta;
        public readonly ref BoundingBox Bounds = ref bounds;
    }
}

public unsafe ref struct VisibleCoreEnumerator
{
    private RenderEntityMeta* _current;
    private RenderEntityMeta* _end;
    private int _entityId;
    private readonly RenderEntityCore _core;

    public VisibleCoreEnumerator(RenderEntityCore core, RenderEntityMeta* entities, int count)
    {
        _current = entities - 1;
        _entityId = 0;
        _end = entities + count;
        _core = core;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        ++_entityId;
        while (++_current < _end)
        {
            if (_current->IsVisible()) return true;
        }

        return false;
    }

    public readonly Item Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(new RenderEntityId(_entityId), ref *_current, _core);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly VisibleCoreEnumerator GetEnumerator() => this;

    public readonly ref struct Item(RenderEntityId entity, ref RenderEntityMeta meta, RenderEntityCore core)
    {
        public readonly RenderEntityId Entity = entity;
        public readonly ref RenderEntityMeta Meta = ref meta;

        public ref RenderSource Source
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref core.GetSource(Entity);
        }

        public ref Matrix4x4 Model
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref core.GetModelMatrix(Entity);
        }

        public ref BoundingBox Bounds
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref core.GetWorldBounds(Entity);
        }
    }
}