using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public sealed class RenderEntityCore : EcsStore
{
    private NativeArray<RenderEntity> _entities;

    private NativeArray<SourceComponent> _sources;
    private NativeArray<Transform> _transforms;
    private NativeArray<BoundingBox> _bounds;
    private NativeArray<Matrix4x4> _matrices;


    internal RenderEntityCore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
        _entities = NativeArray.Allocate<RenderEntity>(initialCapacity);
        _sources = NativeArray.Allocate<SourceComponent>(initialCapacity);
        _transforms = NativeArray.Allocate<Transform>(initialCapacity);
        _bounds = NativeArray.Allocate<BoundingBox>(initialCapacity);
        
        _matrices = NativeArray.AlignedAllocate<Matrix4x4>(initialCapacity, alignment: 16);

        StoreMeta.Listeners.EnsureCapacity(128);
    }

    public override int Capacity => _entities.Length;
    public override EcsStoreType StoreType => EcsStoreType.RenderCore;

    internal NativeView<SourceComponent> GetSourceView() => _sources.Slice(0, Count);
    internal NativeView<Transform> GetTransformView() => _transforms.Slice(0, Count);
    internal NativeView<Matrix4x4> GetMatrixView() => _matrices.Slice(0, Count);
    internal NativeView<BoundingBox> GetBoundsView() => _bounds.Slice(0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(RenderEntityId e)
    {
        var index = e.Index();
        return (uint)index < (uint)_entities.Length && _entities[index].Alive;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(RenderEntityId e) => _entities[e.Index()].IsVisible();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref SourceComponent GetSource(RenderEntityId e) => ref _sources[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Transform GetTransform(RenderEntityId e) => ref _transforms[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoundingBox GetBounds(RenderEntityId e) => ref _bounds[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix4x4 GetMatrix(RenderEntityId e) => ref _matrices[e.Index()];

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityFlags ToggleVisibilityFlag(RenderEntityId entity, VisibilityFlags flag, bool isVisible)
    {
        ref var it = ref _entities[entity.Index()].Visibility;
        if (isVisible) it &= ~flag;
        else it |= flag;
        return it;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public RenderEntityId AddEntity(SourceComponent source, in Transform transform, in BoundingBox bounds)
    {
        var entity = AddEntityInternal(source, in transform, in bounds);
        foreach (var it in StoreMeta.Listeners)
            it.EntityAdded(entity.Id, this);

        return entity;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private RenderEntityId AddEntityInternal(SourceComponent source, in Transform transform, in BoundingBox bounds)
    {
        ValidateSource(source);
        var index = AllocateNext();

        ref var entity = ref _entities[index];
        if (entity.Alive) throw new InvalidOperationException();

        entity.Alive = true;
        entity.Visibility = VisibilityFlags.Visible;
        _sources[index] = source;
        _transforms[index] = transform;
        _bounds[index] = bounds;
        _matrices[index] = Matrix4x4.Identity;

        return new RenderEntityId(index + 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Remove(RenderEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(entity.Id, Count, nameof(entity));

        var index = entity.Index();
        if (!_entities[index].Alive) throw new InvalidOperationException();

        _entities[index] = default;
        _sources[index] = default;
        _transforms[index] = default;
        _bounds[index] = default;
        _matrices[index] = default;

        FreeEntity(index);

        foreach (var it in StoreMeta.Listeners)
            it.EntityRemoved(entity.Id, this);
    }


    protected override void Resize(int newSize)
    {
        var curLen = _entities.Length;
        if (_sources.Length != curLen || _transforms.Length != curLen ||
            _bounds.Length != curLen || _matrices.Length != curLen)
        {
            Throwers.InvalidOperation("Length mismatch");
        }

        _entities.Resize(newSize, true);
        _sources.Resize(newSize, true);
        _transforms.Resize(newSize, true);
        _bounds.Resize(newSize, true);
        _matrices.Resize(newSize, true);

        Logger.LogString(LogScope.Ecs, $"{nameof(RenderEntityCore)}: resized {newSize}", LogLevel.Warn);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ecs.RenderQuery.RenderEntityEnumerator Query() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ecs.RenderQuery.VisibleEntityEnumerator VisibilityQuery() => new(_entities.Slice(0, Count));


    public override void Dispose()
    {
        _entities.Dispose();
        _sources.Dispose();
        _transforms.Dispose();
        _bounds.Dispose();
        _matrices.Dispose();
    }

    [StackTraceHidden]
    private static void ValidateSource(SourceComponent source)
    {
        if (source.Kind == EntitySourceKind.Particle) return;
        ArgumentOutOfRangeException.ThrowIfZero(source.Mesh.Id, nameof(source.Mesh));
        ArgumentOutOfRangeException.ThrowIfZero(source.Material.Id, nameof(source.Material));
        ArgumentOutOfRangeException.ThrowIfEqual((int)source.Kind, (int)EntitySourceKind.Unknown, nameof(source.Kind));
    }
}