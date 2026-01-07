using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS.Data;
using ConcreteEngine.Engine.ECS.Definitions;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS;

public sealed class RenderEntityCore
{
    private static RenderEntityId MakeEntityId() => new(++_count);
    private static int _count;

    private RenderEntityId[] _entities;
    private SourceComponent[] _sources;
    private RenderTransform[] _transforms;
    private BoxComponent[] _boxes;
    private ParentMatrix[] _matrices;

    private readonly Stack<int> _free = [];
    private bool _isDirty;

    internal RenderEntityCore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
        _entities = new RenderEntityId[initialCapacity];
        _sources = new SourceComponent[initialCapacity];
        _transforms = new RenderTransform[initialCapacity];
        _boxes = new BoxComponent[initialCapacity];
        _matrices = new ParentMatrix[initialCapacity];
    }

    public int Count => _count;
    public int ActiveCount => _count - _free.Count;
    public bool IsDirty => _isDirty;

    internal void Initialize()
    {
        InvalidOpThrower.ThrowIf(_entities.Length == 0, nameof(_entities));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(RenderEntityId e)
    {
        var index = e.Index();
        return (uint)index < _entities.Length && _entities[index] == e;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref SourceComponent GetSource(RenderEntityId e) => ref _sources[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderTransform GetTransform(RenderEntityId e) => ref _transforms[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoxComponent GetBox(RenderEntityId e) => ref _boxes[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ParentMatrix GetParentMatrix(RenderEntityId e) => ref _matrices[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<SourceComponent> TryGetSource(RenderEntityId e)
    {
        var id = e.Index();
        if ((uint)id >= _sources.Length) return ValuePtr<SourceComponent>.Null;
        return new ValuePtr<SourceComponent>(ref _sources[id]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<RenderTransform> TryGetTransform(RenderEntityId e)
    {
        var id = e.Index();
        if ((uint)id >= _transforms.Length) return ValuePtr<RenderTransform>.Null;
        return new ValuePtr<RenderTransform>(ref _transforms[id]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TuplePtr<RenderTransform, BoxComponent> TryGetSpatial(RenderEntityId e)
    {
        var index = e.Index();
        if ((uint)index >= _transforms.Length || _transforms.Length != _boxes.Length)
            return TuplePtr<RenderTransform, BoxComponent>.Null;

        return new TuplePtr<RenderTransform, BoxComponent>(ref _transforms[index], ref _boxes[index]);
    }

    // Spans
    public Span<SourceComponent> GetSourceSpan() => _sources.AsSpan(0, _count);
    public Span<RenderTransform> GetTransformSpan() => _transforms.AsSpan(0, _count);
    public Span<BoxComponent> GetBoxSpan() => _boxes.AsSpan(0, _count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderSpatialView GetSpatialView(RenderEntityId e)
    {
        var index = e.Index();
        if ((uint)index >= _transforms.Length || _transforms.Length != _boxes.Length || _transforms.Length != _matrices.Length )
             throw new IndexOutOfRangeException();

        return new RenderSpatialView(ref _transforms[index], ref _boxes[index], ref _matrices[index]);
    }

    // Views
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderEntityContext GetContext()
    {
        var len = _count;
        if ((uint)len > _sources.Length || _sources.Length != _transforms.Length || _sources.Length != _boxes.Length ||
            _sources.Length != _matrices.Length)
            throw new IndexOutOfRangeException();

        return new RenderEntityContext(len, _sources.AsSpan(0, len), _transforms.AsSpan(0, len), _boxes.AsSpan(0, len),
            _matrices.AsSpan(0, len));
    }

    public RenderEntityId AddEntity(in CoreComponentBundle componentBundle)
    {
        if (_free.Count == 0) EnsureCapacity(1);
        var result = AddEntityInternal(in componentBundle);
        _isDirty = true;
        return result;
    }

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

    private RenderEntityId AddEntityInternal(in CoreComponentBundle component)
    {
        ValidateSource(component.Source);

        RenderEntityId entity;
        if (!_free.TryPop(out var index))
        {
            index = _count;
            entity = MakeEntityId();
        }
        else
        {
            entity = new RenderEntityId(index + 1);
        }

        if (entity.Index() != index) throw new InvalidOperationException();

        ref var existingEntity = ref _entities[index];
        if (existingEntity.IsValid()) throw new InvalidOperationException();

        existingEntity = entity;
        _sources[index] = component.Source;
        _transforms[index] = component.Transform;
        _boxes[index] = component.Box;
        _matrices[index] = Matrix4x4.Identity;

        return entity;
    }

    public void Remove(RenderEntityId e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Id, nameof(e));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(e.Id, _count, nameof(e));

        var index = e.Index();
        ref var existing = ref _entities[index];
        if (existing != e) throw new InvalidOperationException();

        _entities[index] = default;
        _sources[index] = default;
        _transforms[index] = default;
        _boxes[index] = default;

        _free.Push(index);
    }

    internal void EndTick()
    {
        _isDirty = false;
    }

    private static void ValidateSource(SourceComponent source)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(source.Model.Value, nameof(source.Model));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(source.MaterialKey.Value, nameof(source.MaterialKey));
        ArgumentOutOfRangeException.ThrowIfEqual((int)source.Kind, (int)EntitySourceKind.Unknown, nameof(source.Kind));
    }

    private void EnsureCapacity(int amount)
    {
        var len = _count + amount;
        if (_entities.Length >= len) return;

        if (_sources.Length != _entities.Length || _transforms.Length != _entities.Length)
        {
            throw new InvalidOperationException();
        }

        var newSize = Arrays.CapacityGrowthSafe(_entities.Length, len);
        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _sources, newSize);
        Array.Resize(ref _transforms, newSize);
        Array.Resize(ref _boxes, newSize);

        Logger.LogString(LogScope.World, $"EntityCoreStore: resized {newSize}", LogLevel.Warn);
    }
}