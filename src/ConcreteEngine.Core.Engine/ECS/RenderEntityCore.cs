using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public sealed class RenderEntityCore
{
    private RenderEntityId MakeEntityId() => new(++_count);
    private int _count;

    private RenderEntityId[] _entities;

    private SourceComponent[] _sources;
    private Transform[] _transforms;
    private BoundingBox[] _bounds;
    private Matrix4x4[] _matrices;
    private VisibilityFlags[] _visibility;

    private readonly Stack<int> _free = [];
    private bool _isDirty;

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

    public int Count => _count;
    public int ActiveCount => _count - _free.Count;
    public int Capacity => _entities.Length;
    public bool IsDirty => _isDirty;

    internal void Initialize()
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
    public Span<Transform> GetTransformSpan() => _transforms.AsSpan(0, _count);

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityFlags ToggleVisibilityFlag(RenderEntityId entity, VisibilityFlags flag, bool isVisible)
    {
        if (isVisible) return _visibility[entity.Index()] &= ~flag;
        return _visibility[entity.Index()] |= flag;
    }

    public RenderEntityId AddEntity(SourceComponent source, in Transform transform, in BoundingBox bounds)
    {
        if (_free.Count == 0) EnsureCapacity(1);
        var result = AddEntityInternal(source, in transform, in bounds);
        _isDirty = true;
        return result;
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
    private RenderEntityId AddEntityInternal(SourceComponent source, in Transform transform, in BoundingBox bounds)
    {
        ValidateSource(source);

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
        _sources[index] = source;
        _transforms[index] = transform;
        _bounds[index] = bounds;
        _matrices[index] = Matrix4x4.Identity;
        _visibility[index] = VisibilityFlags.Visible;

        return entity;
    }

    public void Remove(RenderEntityId e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Id, nameof(e));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(e.Id, _count, nameof(e));

        var index = e.Index();
        var existing = _entities[index];
        if (existing != e) throw new InvalidOperationException();

        _entities[index] = default;
        _sources[index] = default;
        _transforms[index] = default;
        _bounds[index] = default;

        _free.Push(index);
    }

    internal void EndTick()
    {
        _isDirty = false;
    }

    private void EnsureCapacity(int amount)
    {
        var len = _count + amount;
        var curLen = _entities.Length;
        if (curLen >= len) return;

        if (_sources.Length != curLen || _transforms.Length != curLen ||
            _visibility.Length != curLen || _bounds.Length != curLen)
        {
            throw new InvalidOperationException("Length mismatch");
        }

        var newSize = Arrays.CapacityGrowthSafe(curLen, len);
        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _sources, newSize);
        Array.Resize(ref _transforms, newSize);
        Array.Resize(ref _bounds, newSize);
        Array.Resize(ref _visibility, newSize);

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