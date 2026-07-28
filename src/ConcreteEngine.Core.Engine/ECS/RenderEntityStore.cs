using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.ECS.Integration;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public interface IRenderEntityStore;

public sealed class RenderEntityStore<T> : EcsStore, IRenderEntityStore where T : unmanaged, IRenderComponent<T>
{
    private T[] _data;
    private RenderEntityId[] _entities;

    private readonly List<IRenderComponentListener<T>> _listeners = new(32);

    public RenderEntityStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 16);
        _data = new T[initialCapacity];
        _entities = new RenderEntityId[initialCapacity];
    }

    public override int Capacity => _entities.Length;
    public override EcsStoreType StoreType => EcsStoreType.Render;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(RenderEntityId entity) => FindIndex(entity) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderEntityId GetEntity(int i) => _entities[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetByIndex(int i) => ref _data[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(RenderEntityId entity) => ref _data[FindIndex(entity)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrDefault(RenderEntityId entity)
    {
        var id = FindIndex(entity);
        if ((uint)id >= _data.Length) return default;
        return _data[id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<T> TryGet(RenderEntityId entity)
    {
        var index = FindIndex(entity);
        if ((uint)index >= (uint)_entities.Length) return ValuePtr<T>.Null;
        return new ValuePtr<T>(ref _data[index]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<RenderEntityId> GetEntitySpan() => _entities.AsSpan(0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetComponentSpan() => _data.AsSpan(0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(RenderEntityId entity)
    {
        return MemoryMarshal.Cast<RenderEntityId, int>(GetEntitySpan()).IndexOf(entity.Id);
        
    }//SearchMethod.BinarySearch(GetEntitySpan(), entity);

    public bool Add(RenderEntityId entity, in T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        if (Has(entity)) return false;
        var index = AllocateNext();

        _entities[index] = entity;
        _data[index] = value;

        ref var data = ref _data[index];
        foreach (var it in _listeners)
            it.ComponentAdded(entity.Id, ref data);

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool Remove(RenderEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));

        var index = FindIndex(entity);
        if (index == -1) return false;

        ref var data = ref _data[index];
        foreach (var it in _listeners)
            it.ComponentRemoved(entity.Id, ref data);

        _entities[index] = default;
        data = default;
        FreeEntity(index);

        return true;
    }


    public void BindListener(IRenderComponentListener<T> listener) => _listeners.Add(listener);
    public void UnbindListener(IRenderComponentListener<T> listener) => _listeners.Remove(listener);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityEnumerator VisibilityQuery() => new(this, Ecs.RenderCore);

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected override void Resize(int newSize)
    {
        if (_data.Length != _entities.Length)
            Throwers.InvalidOperation("Length mismatch");

        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _data, newSize);

        Logger.Log(LogScope.Ecs, $"{GetType().Name}: resized {newSize}", LogLevel.Warn);
    }

    public override void Dispose() { }
    
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
        public readonly Enumerator GetEnumerator() => new(store);
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
        public readonly VisibilityEnumerator GetEnumerator() => new(store, core);
    }

}