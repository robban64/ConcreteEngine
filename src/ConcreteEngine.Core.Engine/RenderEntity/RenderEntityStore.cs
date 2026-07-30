using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public interface IRenderEntityStore;

public sealed class RenderEntityStore<T> : IRenderEntityStore where T : unmanaged, IRenderComponent<T>
{
    public int Count { get; private set; }

    private bool _isDirty;

    private T[] _data;
    private RenderEntityId[] _entities;

    private readonly List<RenderEntityId> _removedEntities = [];
    private readonly List<IRenderComponentListener<T>> _listeners = [];

    public RenderEntityStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 16);
        _data = new T[initialCapacity];
        _entities = new RenderEntityId[initialCapacity];
    }

    public int Capacity => _entities.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndexSorted(RenderEntityId entity) => SearchMethod.BinarySearch(GetEntitySpan(), entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndexLinear(RenderEntityId entity)
        => MemoryMarshal.Cast<RenderEntityId, int>(GetEntitySpan()).IndexOf(entity.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(RenderEntityId entity) => FindIndexLinear(entity) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderEntityId GetEntity(int i) => _entities[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetByIndex(int i) => ref _data[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(RenderEntityId entity) => ref _data[FindIndexSorted(entity)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrDefault(RenderEntityId entity)
    {
        var index = FindIndexSorted(entity);
        if ((uint)index >= (uint)_data.Length) return default;
        return _data[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(RenderEntityId entity, out ValueRef<T> value)
    {
        var index = FindIndexLinear(entity);
        if ((uint)index < (uint)_entities.Length)
        {
            value = new ValueRef<T>(ref _data[index]);
            return true;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<RenderEntityId> GetEntitySpan() => _entities.AsSpan(0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetComponentSpan() => _data.AsSpan(0, Count);


    public bool Add(RenderEntityId entity, in T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        if (Has(entity)) return false;
        if (Count >= Capacity) EnsureCapacity(1);

        var index = Count++;

        _entities[index] = entity;
        _data[index] = value;

        if (_listeners.Count > 0)
        {
            foreach (var listener in CollectionsMarshal.AsSpan(_listeners))
                listener.ComponentAdded(entity, ref _data[index]);
        }

        _isDirty = true;
        return true;
    }

    public bool Remove(RenderEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        if (!Has(entity)) return false;
        _removedEntities.Add(entity);
        _isDirty = true;
        return true;
    }

    public void Commit()
    {
        if (!_isDirty) return;
        _isDirty = false;

        if (_removedEntities.Count > 0) CommitRemoved();

        GetEntitySpan().Sort(GetComponentSpan());
        return;

        void CommitRemoved()
        {
            foreach (var entity in CollectionsMarshal.AsSpan(_removedEntities))
            {
                var index = FindIndexLinear(entity);
                if (_listeners.Count > 0)
                {
                    foreach (var listener in CollectionsMarshal.AsSpan(_listeners))
                        listener.ComponentRemoved(entity, ref _data[index]);
                }

                var count = --Count;
                _entities[index] = _entities[count];
                _data[index] = _data[count];

                _entities[count] = default;
                _data[count] = default;
            }

            _removedEntities.Clear();
        }
    }

    public void BindListener(IRenderComponentListener<T> listener) => _listeners.Add(listener);
    public void UnbindListener(IRenderComponentListener<T> listener) => _listeners.Remove(listener);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityEnumerator VisibilityQuery() => new(this, RenderEcs.Core);

    public void EnsureCapacity(int amount)
    {
        var length = Count + amount;
        if (Capacity >= length) return;

        var newSize = CapacityUtils.CapacityGrowthToFit(Capacity, length);
        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _data, newSize);

        Logger.Log(LogScope.Ecs, $"{typeof(T)}: resized {newSize}", LogLevel.Warn);
    }

    public void Dispose() { }

    public readonly ref struct RenderQueryItem(int idx, RenderEntityId entityId, ref T component)
    {
        public readonly ref T Component = ref component;
        public readonly int Index = idx;
        public readonly RenderEntityId Entity = entityId;
    }

    public ref struct Enumerator(RenderEntityStore<T> store)
    {
        private int _i = -1;
        private RenderEntityId _currentEntity;
        private readonly int _count = store.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _count)
            {
                var entity = store.GetEntity(_i);
                if (entity.IsValid())
                {
                    _currentEntity = entity;
                    return true;
                }
            }

            return false;
        }

        public readonly RenderQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_i, _currentEntity, ref store.GetByIndex(_i));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Enumerator GetEnumerator() => this;
    }


    public ref struct VisibilityEnumerator(RenderEntityStore<T> store, RenderEntityCore core)
    {
        private int _i = -1;
        private RenderEntityId _currentEntity;
        private readonly int _count = store.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _count)
            {
                var entity = store.GetEntity(_i);
                if (entity.Id > 0 && core.IsVisible(entity))
                {
                    _currentEntity = entity;
                    return true;
                }
            }

            return false;
        }

        public readonly RenderQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_i, _currentEntity, ref store.GetByIndex(_i));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly VisibilityEnumerator GetEnumerator() => this;
    }
}