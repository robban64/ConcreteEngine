using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public sealed class RenderEntityCore
{
    public int Count { get; private set; }
    
    private NativeArray<RenderEntityMeta> _entityMeta;
    private NativeSoA<RenderSource, DrawPolicy> _sources;
    private NativeSoA<Matrix4x4, Matrix3X4> _matrices;
    private NativeArray<BoundingBox> _bounds;

    private readonly Stack<int> _free = [];
    private readonly List<Action<int>> _resizeCallbacks = [];

    internal RenderEntityCore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
        _entityMeta = NativeArray.Allocate<RenderEntityMeta>(initialCapacity);
        _sources = new NativeSoA<RenderSource, DrawPolicy>(initialCapacity);
        _matrices = new NativeSoA<Matrix4x4, Matrix3X4>(initialCapacity);
        _bounds = NativeArray.Allocate<BoundingBox>(initialCapacity);
    }

    public int ActiveCount => Count - _free.Count;
    public int Capacity => _entityMeta.Length;
    
    public void AddResizeCallback(Action<int> callback) => _resizeCallbacks.Add(callback);
    public void RemoveResizeCallback(Action<int> callback) => _resizeCallbacks.Remove(callback);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(RenderEntityId e)
    {
        var index = e.Index();
        return (uint)index < (uint)_entityMeta.Length && _entityMeta[index].Alive;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(RenderEntityId e)
    {
        var index = e.Index();
        return (uint)index < (uint)_entityMeta.Length && _entityMeta[index].IsVisible();
    }

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderEntityMeta GetMeta(RenderEntityId e) => ref _entityMeta[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderSource GetSource(RenderEntityId e) => ref _sources.At1(e.Index());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawPolicy GetDrawPolicy(RenderEntityId e) => ref _sources.At2(e.Index());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoundingBox GetWorldBounds(RenderEntityId e) => ref _bounds[e.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix4x4 GetModelMatrix(RenderEntityId e) => ref _matrices.At1(e.Index());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix3X4 GetNormalMatrix(RenderEntityId e) => ref _matrices.At2(e.Index());

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityVisibility ToggleVisibility(RenderEntityId entity, EntityVisibility flag, bool isVisible)
    {
        if (!IsAlive(entity)) Throwers.InvalidOperation(nameof(entity));
        return _entityMeta[entity.Index()].ToggleVisibility(flag, isVisible);
    }

    public RenderEntityId AddEntity(RenderSource source, DrawPolicy policy)
    {
        ValidateSource(source);

        var entity = AllocateNewEntity();
        var index = entity.Index();

        _sources.Set(index, source, policy);
        _matrices.At1(index) = Matrix4x4.Identity;
        _matrices.At2(index) = Matrix3X4.Identity;
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

        ref var entity = ref _entityMeta[index];
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
        if (!_entityMeta[index].Alive) throw new InvalidOperationException();

        _entityMeta[index] = default;
        _sources.Set(index, default, default);
        _matrices.At1(index) = Matrix4x4.Identity;
        _matrices.At2(index) = Matrix3X4.Identity;
        _bounds[index] = BoundingBox.One;

        Count = SlotHelper.FreeSlot(_free, index, Count);
    }


    private void EnsureCapacity(int amount)
    {
        var length = _entityMeta.Length;
        var required = Count + amount;
        if (length >= required) return;

        if (_sources.Length != length || _matrices.Length != length || _bounds.Length != length)
            Throwers.InvalidOperation("Length mismatch");

        var newSize = CapacityUtils.CapacityGrowthToFit(length, required);
        Logger.Log(LogScope.Ecs, $"{nameof(RenderEntityCore)}: resized {newSize}", LogLevel.Warn);

        _entityMeta.Resize(newSize, true);
        _sources.Resize(newSize, true);
        _matrices.Resize(newSize, false);
        _bounds.Resize(newSize, false);
        
        foreach (var callback in _resizeCallbacks) callback(newSize);

    }


    public void Dispose()
    {
        _entityMeta.Dispose();
        _sources.Dispose();
        _matrices.Dispose();
        _bounds.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Enumerator GetEnumerator() => new(_entityMeta, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibleCoreEnumerator VisibilityQuery() => new(this, _entityMeta.AsReadOnlySpan(0, Count));

    [StackTraceHidden]
    private static void ValidateSource(RenderSource source)
    {
        if (source.Kind == EntitySourceKind.Particle) return;
        ArgumentOutOfRangeException.ThrowIfZero(source.Mesh.Id, nameof(source.Mesh));
        ArgumentOutOfRangeException.ThrowIfZero(source.Material.Value, nameof(source.Material));
        ArgumentOutOfRangeException.ThrowIfEqual((int)source.Kind, (int)EntitySourceKind.Unknown, nameof(source.Kind));
    }


    public readonly ref struct RenderQueryItem(RenderEntityId entity, RenderEntityCore core)
    {
        public readonly RenderEntityId Entity = entity;
        public ref RenderEntityMeta Meta => ref core.GetMeta(Entity);

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

    public readonly ref struct RenderQueryItem2(RenderEntityId entity, ref RenderEntityMeta meta)
    {
        public readonly RenderEntityId Entity = entity;
        public readonly ref RenderEntityMeta Meta = ref meta;
    }

    public unsafe ref struct Enumerator(RenderEntityMeta* entities, int length)
    {
        private int _i = -1;
        private readonly int _length = length;
        private readonly RenderEntityMeta* _entities = entities;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _length)
            {
                if (_entities[_i].Alive) return true;
            }

            return false;
        }

        public readonly RenderQueryItem2 Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(new RenderEntityId(_i + 1), ref _entities[_i]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Enumerator GetEnumerator() => this;
    }

    public ref struct VisibleCoreEnumerator(RenderEntityCore core, ReadOnlySpan<RenderEntityMeta> entities)
    {
        private int _i = -1;
        private readonly ReadOnlySpan<RenderEntityMeta> _entities = entities;
        private readonly RenderEntityCore _core = core;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _entities.Length)
            {
                if (_entities[_i].IsVisible()) return true;
            }

            return false;
        }

        public readonly RenderQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(new RenderEntityId(_i + 1), _core);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly VisibleCoreEnumerator GetEnumerator() => new(_core, _entities);
    }
}