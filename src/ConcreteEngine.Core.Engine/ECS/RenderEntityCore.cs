using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using static ConcreteEngine.Core.Engine.ECS.Ecs.RenderQuery;

namespace ConcreteEngine.Core.Engine.ECS;

public sealed class RenderEntityCore : EcsStore
{
    private NativeArray<RenderEntity> _entities;
    private NativeArray<SourceComponent> _sources;
    private NativeArray<BoundingBox> _worldBounds;
    private NativeArray<Matrix4x4> _modelMatrices;
    private NativeArray<Matrix3X4> _normalMatrices;

    internal RenderEntityCore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
        _entities = NativeArray.Allocate<RenderEntity>(initialCapacity);
        _sources = NativeArray.Allocate<SourceComponent>(initialCapacity);
        _worldBounds = NativeArray.Allocate<BoundingBox>(initialCapacity);
        _modelMatrices = NativeArray.AlignedAllocate<Matrix4x4>(initialCapacity);
        _normalMatrices = NativeArray.Allocate<Matrix3X4>(initialCapacity);
        
        StoreMeta.Listeners.EnsureCapacity(128);
    }

    public override int Capacity => _entities.Length;
    public override EcsStoreType StoreType => EcsStoreType.RenderCore;

    internal NativeView<RenderEntity> GetCoreEntityView() => _entities.Slice(0, Count);
    internal NativeView<SourceComponent> GetSourceView() => _sources.Slice(0, Count);
    internal NativeView<BoundingBox> GetWorldBoundsView() => _worldBounds.Slice(0, Count);
    internal NativeView<Matrix4x4> GetModelView() => _modelMatrices.Slice(0, Count);
    internal NativeView<Matrix3X4> GetNormalsView() => _normalMatrices.Slice(0, Count);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(RenderEntityId e)
    {
        var index = e.Index();
        return (uint)index < (uint)_entities.Length && _entities[index].Alive;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(RenderEntityId e) => _entities[e.Index()].Alive;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(RenderEntityId e) => _entities[e.Index()].IsVisible();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderEntity GetCoreEntity(RenderEntityId e) => ref _entities[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref SourceComponent GetSource(RenderEntityId e) => ref _sources[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoundingBox GetWorldBounds(RenderEntityId e) => ref _worldBounds[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix4x4 GetModelMatrix(RenderEntityId e) => ref _modelMatrices[e.Index()];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix3X4 GetNormalMatrix(RenderEntityId e) => ref _normalMatrices[e.Index()];

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityFlags ToggleVisibility(RenderEntityId entity, VisibilityFlags flag, bool isVisible)
    {
        return _entities[entity.Index()].ToggleVisibility(flag, isVisible);
    }

    public RenderEntityId Copy(RenderEntityId entity)
    {
        var newEntity = AllocateNewEntity();
        GetSource(newEntity) = GetSource(entity);
        GetWorldBounds(newEntity) = GetWorldBounds(entity);
        GetModelMatrix(newEntity) = GetModelMatrix(entity);
        return newEntity;
    }

    public RenderEntityId AddEntity(SourceComponent source)
    {
        ValidateSource(source);

        var entity = AllocateNewEntity();
        _sources[entity.Index()] = source;
        _worldBounds[entity.Index()] = BoundingBox.One;
        _modelMatrices[entity.Index()] = Matrix4x4.Identity;
        _normalMatrices[entity.Index()] = Matrix3X4.Identity;

        foreach (var it in StoreMeta.Listeners)
            it.EntityAdded(entity.Id, this);

        return entity;
    }


    private RenderEntityId AllocateNewEntity()
    {
        var index = AllocateNext();
        ref var entity = ref _entities[index];
        if (entity.Alive) Throwers.InvalidOperation($"Entity {entity} already exists");
        entity.Alive = true;
        entity.Visibility = VisibilityFlags.Visible;
        return new RenderEntityId(index + 1);
    }

    public void Remove(RenderEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(entity.Id, Count, nameof(entity));

        var index = entity.Index();
        if (!_entities[index].Alive) throw new InvalidOperationException();

        _entities[index] = default;
        _sources[index] = default;
        _worldBounds[index] = default;
        _modelMatrices[index] = default;
        _normalMatrices[index] = default;
        FreeEntity(index);

        foreach (var it in StoreMeta.Listeners)
            it.EntityRemoved(entity.Id, this);
    }


    protected override void Resize(int newSize)
    {
        var curLen = _entities.Length;
        if (_sources.Length != curLen || _worldBounds.Length != curLen || _modelMatrices.Length != curLen || _normalMatrices.Length != curLen)
        {
            Throwers.InvalidOperation("Length mismatch");
        }

        _entities.Resize(newSize, true);
        _sources.Resize(newSize, true);
        _worldBounds.Resize(newSize, true);
        _modelMatrices.Resize(newSize, true);
        _normalMatrices.Resize(newSize, true);

        Logger.LogString(LogScope.Ecs, $"{nameof(RenderEntityCore)}: resized {newSize}", LogLevel.Warn);
    }


    public override void Dispose()
    {
        _entities.Dispose();
        _sources.Dispose();
        _worldBounds.Dispose();
        _modelMatrices.Dispose();
        _normalMatrices.Dispose();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public  VisibleCoreEnumerator VisibilityQuery() => new(this);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibleCoreEnumerator<T1> VisibleQuery<T1>(NativeView<T1> view1) 
        where T1 : unmanaged 
    {
        return new VisibleCoreEnumerator<T1>(GetCoreEntityView(), view1);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibleCoreEnumerator<T1, T2> VisibleQuery<T1, T2>(NativeView<T1> view1, NativeView<T2> view2) 
        where T1 : unmanaged where T2 : unmanaged
    {
        return new VisibleCoreEnumerator<T1, T2>(GetCoreEntityView(), view1, view2);
    }

    [StackTraceHidden]
    private static void ValidateSource(SourceComponent source)
    {
        if (source.Kind == EntitySourceKind.Particle) return;
        ArgumentOutOfRangeException.ThrowIfZero(source.Mesh.Id, nameof(source.Mesh));
        ArgumentOutOfRangeException.ThrowIfZero(source.Material.Value, nameof(source.Material));
        ArgumentOutOfRangeException.ThrowIfEqual((int)source.Kind, (int)EntitySourceKind.Unknown, nameof(source.Kind));
    }
}