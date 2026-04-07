using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.ECS.Integration;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public sealed class RenderEntityCore : EcsStore
{
    private NativeArray<RenderEntityId> _entities;

    private NativeArray<SourceComponent> _sources;
    private NativeArray<Transform> _transforms;
    private NativeArray<BoundingBox> _bounds;
    private NativeArray<Matrix4x4> _matrices;
    private NativeArray<byte> _visibility;

    private readonly List<IEntityListener> _listeners = new(128);

    internal RenderEntityCore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
        _entities = NativeArray.Allocate<RenderEntityId>(initialCapacity);
        _sources = NativeArray.Allocate<SourceComponent>(initialCapacity);
        _transforms = NativeArray.Allocate<Transform>(initialCapacity);
        _bounds = NativeArray.Allocate<BoundingBox>(initialCapacity);
        _matrices = NativeArray.Allocate<Matrix4x4>(initialCapacity);
        _visibility = NativeArray.Allocate<byte>(initialCapacity);
    }

    public override int Capacity => _entities.Length;
    public override EcsStoreType StoreType => EcsStoreType.RenderCore;

    internal override void Initialize()
    {
        InvalidOpThrower.ThrowIf(_entities.Length == 0, nameof(_entities));
    }

    internal NativeView<SourceComponent> GetSourceView() => _sources.Slice(0, Count);
    internal NativeView<Transform> GetTransformView() => _transforms.Slice(0, Count);
    internal NativeView<Matrix4x4> GetMatrixView() => _matrices.Slice(0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(RenderEntityId e)
    {
        var index = e.Index();
        return (uint)index < (uint)_entities.Length && _entities[index] == e;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderEntityId GetByIndex(int index) => _entities[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(RenderEntityId e) => _visibility[e.Index()] == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref SourceComponent GetSource(RenderEntityId e) => ref _sources[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Transform GetTransform(RenderEntityId e) => ref _transforms[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoundingBox GetBounds(RenderEntityId e) => ref _bounds[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix4x4 GetParentMatrix(RenderEntityId e) => ref _matrices[e.Index()];

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityFlags ToggleVisibilityFlag(RenderEntityId entity, VisibilityFlags flag, bool isVisible)
    {
        ref var it = ref _visibility[entity.Index()];
        if (isVisible) it &= (byte)~flag;
        else it |= (byte)flag;
        return (VisibilityFlags)it;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ecs.RenderQuery.RenderEntityEnumerator Query() => new(this);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public RenderEntityId AddEntity(SourceComponent source, in Transform transform, in BoundingBox bounds)
    {
        var entity = AddEntityInternal(source, in transform, in bounds);
        foreach (var it in _listeners)
            it.EntityAdded(entity, this);

        return entity;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private RenderEntityId AddEntityInternal(SourceComponent source, in Transform transform, in BoundingBox bounds)
    {
        ValidateSource(source);
        var index = AllocateNext();
        var entity = new RenderEntityId(index + 1);

        ref var existingEntity = ref _entities[index];
        if (existingEntity.IsValid()) throw new InvalidOperationException();

        existingEntity = entity;
        _sources[index] = source;
        _transforms[index] = transform;
        _bounds[index] = bounds;
        _matrices[index] = Matrix4x4.Identity;
        _visibility[index] = (byte)VisibilityFlags.Visible;

        return entity;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Remove(RenderEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(entity.Id, Count, nameof(entity));

        var index = entity.Index();
        var existing = _entities[index];
        if (existing != entity) throw new InvalidOperationException();

        _entities[index] = default;
        _sources[index] = default;
        _transforms[index] = default;
        _bounds[index] = default;
        _matrices[index] = default;
        _visibility[index] = 0;

        FreeEntity(index);

        foreach (var it in _listeners)
            it.EntityRemoved(entity, this);
    }

    public void BindListener(IEntityListener listener) => _listeners.Add(listener);
    public void UnbindListener(IEntityListener listener) => _listeners.Remove(listener);

    protected override void Resize(int newSize)
    {
        var curLen = _entities.Length;
        if (_sources.Length != curLen || _transforms.Length != curLen ||
            _visibility.Length != curLen || _bounds.Length != curLen ||
            _matrices.Length != curLen)
        {
            throw new InvalidOperationException("Length mismatch");
        }

        _entities.Resize(newSize, true);
        _sources.Resize(newSize, true);
        _transforms.Resize(newSize, true);
        _bounds.Resize(newSize, true);
        _matrices.Resize(newSize, true);
        _visibility.Resize(newSize, true);

        Console.WriteLine($"EntityCoreStore: resized {newSize}");
        //Logger.LogString(LogScope.World, $"EntityCoreStore: resized {newSize}", LogLevel.Warn);
    }


    [StackTraceHidden]
    private static void ValidateSource(SourceComponent source)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(source.Mesh.Value, nameof(source.Mesh));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(source.Material.Id, nameof(source.Material));
        ArgumentOutOfRangeException.ThrowIfEqual((int)source.Kind, (int)EntitySourceKind.Unknown, nameof(source.Kind));
    }
}