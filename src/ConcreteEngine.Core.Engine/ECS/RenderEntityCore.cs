using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.ECS.Abstract;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS;


public sealed class RenderEntityCore : EcsStore
{
    private RenderEntityId[] _entities;

    private SourceComponent[] _sources;
    private Transform[] _transforms;
    private BoundingBox[] _bounds;
    private Matrix4x4[] _matrices;
    private VisibilityFlags[] _visibility;

    private readonly List<IEntityListener> _listeners = new(128);

    internal RenderEntityCore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
        _entities = new RenderEntityId[initialCapacity];
        _sources = new SourceComponent[initialCapacity];
        _transforms = new Transform[initialCapacity];
        _bounds = new BoundingBox[initialCapacity];
        _matrices = new Matrix4x4[initialCapacity];
        _visibility = new VisibilityFlags[initialCapacity];
    }

    public override int Capacity => _entities.Length;
    public override EcsStoreType StoreType => EcsStoreType.RenderCore;

    internal override void Initialize()
    {
        InvalidOpThrower.ThrowIf(_entities.Length == 0, nameof(_entities));
    }


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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<SourceComponent> TryGetSource(RenderEntityId e)
    {
        var id = e.Index();
        if ((uint)id >= (uint)_sources.Length) return ValuePtr<SourceComponent>.Null;
        return new ValuePtr<SourceComponent>(ref _sources[id]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<Transform> TryGetTransform(RenderEntityId e)
    {
        var id = e.Index();
        if ((uint)id >= (uint)_transforms.Length) return ValuePtr<Transform>.Null;
        return new ValuePtr<Transform>(ref _transforms[id]);
    }

    // 
    public Span<Transform> GetTransformSpan() => _transforms.AsSpan(0, Count);

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityFlags ToggleVisibilityFlag(RenderEntityId entity, VisibilityFlags flag, bool isVisible)
    {
        if (isVisible) return _visibility[entity.Index()] &= ~flag;
        return _visibility[entity.Index()] |= flag;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public RenderEntityId AddEntity(SourceComponent source, in Transform transform, in BoundingBox bounds)
    {
        var entity = AddEntityInternal(source, in transform, in bounds);
        foreach (var it in _listeners)
            it.EntityAdded(entity, this);

        return entity;
    }

/*
    public void AddEntities(ReadOnlySpan<CoreComponentBundle> components, Span<RenderEntityId> result)
    {
        int ensureCap = int.Max(0, components.Length - _free.Count);
        if (ensureCap > 0)
            EnsureCapacity(ensureCap);

        for (var i = 0; i < components.Length; i++)
        {
            ref readonly var component = ref components[i];
            result[i] = AddEntityInternal(in component);
        }

        _isDirty = true;
    }
*/

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
        _visibility[index] = VisibilityFlags.Visible;

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

        FreeEntity(index, entity);

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

        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _sources, newSize);
        Array.Resize(ref _transforms, newSize);
        Array.Resize(ref _bounds, newSize);
        Array.Resize(ref _matrices, newSize);
        Array.Resize(ref _visibility, newSize);
    }


    [StackTraceHidden]
    private static void ValidateSource(SourceComponent source)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(source.Mesh.Value, nameof(source.Mesh));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(source.Material.Id, nameof(source.Material));
        ArgumentOutOfRangeException.ThrowIfEqual((int)source.Kind, (int)EntitySourceKind.Unknown, nameof(source.Kind));
    }
}