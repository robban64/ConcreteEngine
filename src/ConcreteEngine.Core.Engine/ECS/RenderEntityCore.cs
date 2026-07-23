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
    private NativeSoA<BoundingBox, Matrix4x4, Matrix3X4> _spatial;

    internal RenderEntityCore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
        _entities = NativeArray.Allocate<RenderEntity>(initialCapacity);
        _sources = NativeArray.Allocate<SourceComponent>(initialCapacity);
        _spatial = new NativeSoA<BoundingBox, Matrix4x4, Matrix3X4>(initialCapacity);
        StoreMeta.Listeners.EnsureCapacity(128);
    }

    public override int Capacity => _entities.Length;
    public override EcsStoreType StoreType => EcsStoreType.RenderCore;

    internal NativeView<RenderEntity> GetCoreEntityView() => _entities.Slice(0, Count);
    internal NativeView<SourceComponent> GetSourceView() => _sources.Slice(0, Count);
    internal NativeView<BoundingBox> GetWorldBoundsView() => _spatial.View1.Slice(0, Count);
    internal NativeView<Matrix4x4> GetModelView() => _spatial.View2.Slice(0, Count);
    internal NativeView<Matrix3X4> GetNormalsView() => _spatial.View3.Slice(0, Count);


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
    public ref BoundingBox GetWorldBounds(RenderEntityId e) => ref _spatial.At1(e.Index());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix4x4 GetModelMatrix(RenderEntityId e) => ref _spatial.At2(e.Index());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix3X4 GetNormalMatrix(RenderEntityId e) => ref _spatial.At3(e.Index());

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityFlags ToggleVisibility(RenderEntityId entity, VisibilityFlags flag, bool isVisible)
    {
        return _entities[entity.Index()].ToggleVisibility(flag, isVisible);
    }

    public RenderEntityId AddEntity(SourceComponent source)
    {
        ValidateSource(source);

        var entity = AllocateNewEntity();
        _sources[entity.Index()] = source;
        
        _spatial.Set(entity.Index(), BoundingBox.One, Matrix4x4.Identity, Matrix3X4.Identity);

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
        _spatial.Set(entity.Index(), default, default, default);

        FreeEntity(index);

        foreach (var it in StoreMeta.Listeners)
            it.EntityRemoved(entity.Id, this);
    }


    protected override void Resize(int newSize)
    {
        var curLen = _entities.Length;
        if (_sources.Length != curLen || _spatial.Length != curLen)
        {
            Throwers.InvalidOperation("Length mismatch");
        }

        _entities.Resize(newSize, true);
        _sources.Resize(newSize, true);
        _spatial.Resize(newSize, true);
        Logger.Log(LogScope.Ecs, $"{nameof(RenderEntityCore)}: resized {newSize}", LogLevel.Warn);
    }


    public override void Dispose()
    {
        _entities.Dispose();
        _sources.Dispose();
        _spatial.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibleCoreEnumerator VisibilityQuery() => new(this);

    [StackTraceHidden]
    private static void ValidateSource(SourceComponent source)
    {
        if (source.Kind == EntitySourceKind.Particle) return;
        ArgumentOutOfRangeException.ThrowIfZero(source.Mesh.Id, nameof(source.Mesh));
        ArgumentOutOfRangeException.ThrowIfZero(source.Material.Value, nameof(source.Material));
        ArgumentOutOfRangeException.ThrowIfEqual((int)source.Kind, (int)EntitySourceKind.Unknown, nameof(source.Kind));
    }
}