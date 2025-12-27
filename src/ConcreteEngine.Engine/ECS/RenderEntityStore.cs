using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS;

public interface IRenderEntityStore
{
    void EndTick();
}

public sealed class RenderEntityStore<T> : IRenderEntityStore where T : unmanaged, IRenderComponent<T>
{
    private T[] _data;
    private RenderEntityId[] _entities;

    private readonly Stack<int> _free = [];

    private int _count;
    private bool _isDirty;

    public int Count => _count;
    public int ActiveCount => Count - _free.Count;
    public bool IsDirty => _isDirty;

    public RenderEntityStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 16);
        _data = new T[initialCapacity];
        _entities = new RenderEntityId[initialCapacity];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(RenderEntityId renderEntity) => FindIndex(renderEntity) >= 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderEntityId GetEntity(int i) => _entities[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(RenderEntityId renderEntity) => ref _data[FindIndex(renderEntity)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetByIndex(int i) => ref _data[i];

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
        var id = FindIndex(entity);
        if ((uint)id >= _data.Length) return ValuePtr<T>.Null;
        return new ValuePtr<T>(ref _data[id]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<RenderEntityId> GetEntitySpan() => _entities.AsSpan(0, _count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetComponentSpan() => _data.AsSpan(0, _count);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(RenderEntityId renderEntity) => SortMethod.BinarySearch(GetEntitySpan(), renderEntity);

    public void Add(RenderEntityId renderEntity, T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(renderEntity.Id, nameof(renderEntity));
        if (!_free.TryPop(out var index))
        {
            EnsureCapacity(1);
            index = _count++;
        }

        _entities[index] = renderEntity;
        _data[index] = value;
        _isDirty = true;
    }

    public void Remove(RenderEntityId renderEntity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(renderEntity.Id, nameof(renderEntity));

        var idx = FindIndex(renderEntity);
        if (idx == -1) throw new ArgumentOutOfRangeException(nameof(renderEntity));

        _entities[idx] = default;
        _data[idx] = default;
        _free.Push(idx);
    }

    public void EndTick() => _isDirty = false;


    private void EnsureCapacity(int amount)
    {
        var len = _count + amount;
        if (_entities.Length >= len) return;

        if (_data.Length != _entities.Length)
        {
            throw new InvalidOperationException();
        }


        var newSize = Arrays.CapacityGrowthSafe(_entities.Length, len);
        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _data, newSize);
        Logger.LogString(LogScope.World, $"EntityStore: {typeof(T).Name} resized {newSize}", LogLevel.Warn);
    }
}