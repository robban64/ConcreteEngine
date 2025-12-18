using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.ECS.Game;

namespace ConcreteEngine.Engine.ECS.Data;

public ref struct GameEntityEnumerator<T1>(GameEntityStore<T1> r)
    where T1 : unmanaged
{
    private int _i = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < r.Count;

    public Item Current => new(_i, r);

    public readonly ref struct Item(int idx, GameEntityStore<T1> r)
    {
        public readonly int Index = idx;
        public GameEntityId Entity => r.GetEntity(Index);
        public ref T1 Component => ref r.GetByIndex(Index);
    }

    public GameEntityEnumerator<T1> GetEnumerator()
    {
        _i = -1;
        return this;
    }
}

public ref struct GameEntityEnumerator<T1, T2>(GameEntityStore<T1> r1, GameEntityStore<T2> r2)
    where T1 : unmanaged where T2 : unmanaged
{
    private int _i = -1;
    private readonly int _count = r1.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        var entity = r1.GetEntity(++_i);
        return r2.Has(entity);
    }

    public Item Current => new(_i, r1, r2);

    public readonly ref struct Item(int idx, GameEntityStore<T1> r1, GameEntityStore<T2> r2)
    {
        public readonly int Index = idx;
        public readonly GameEntityId Entity = r1.GetEntity(idx);
        public ref T1 Component1 => ref r1.GetByIndex(Index);
        public ref T2 Component2 => ref r2.Get(Entity);
    }

    public GameEntityEnumerator<T1, T2> GetEnumerator()
    {
        _i = -1;
        return this;
    }
}