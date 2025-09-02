namespace ConcreteEngine.Core.Scene;

public struct EntityEnumerator<T1>(GameComponentStore<T1> r)
    where T1 : struct
{
    private int _i = -1;

    public bool MoveNext() => ++_i < r.Count;
    public Item Current => new Item(r.EntityByIndex(_i), _i, r);

    public readonly ref struct Item(GameEntityId e, int idx, GameComponentStore<T1> r)
    {
        public readonly GameEntityId Entity = e;
        public readonly int Index = idx;
        public ref T1 Value => ref r.ByIndex(Index);
    }

    public EntityEnumerator<T1> GetEnumerator() => this;
}

public struct EntityEnumerator<T1, T2>(GameComponentStore<T1> r1, GameComponentStore<T2> r2)
    where T1 : struct where T2 : struct
{
    private int _i = -1;

    public bool MoveNext() => ++_i < r1.Count;
    public Item Current => new Item(r1.EntityByIndex(_i), _i, r1, r2);

    public readonly ref struct Item(
        GameEntityId e,
        int idx,
        GameComponentStore<T1> r1,
        GameComponentStore<T2> r2)
    {
        public readonly GameEntityId Entity = e;
        public readonly int Index = idx;
        public ref T1 Value1 => ref r1.ByIndex(Index);
        public ref T2 Value2 => ref r2.ByIndex(Index);
    }

    public EntityEnumerator<T1, T2> GetEnumerator() => this;
}

public struct EntityEnumerator<T1, T2, T3>(
    GameComponentStore<T1> r1,
    GameComponentStore<T2> r2,
    GameComponentStore<T3> r3)
    where T1 : struct where T2 : struct where T3 : struct
{
    private int _i = -1;

    public bool MoveNext() => ++_i < r1.Count;
    public Item Current => new Item(r1.EntityByIndex(_i), _i, r1, r2, r3);

    public readonly ref struct Item(
        GameEntityId e,
        int idx,
        GameComponentStore<T1> r1,
        GameComponentStore<T2> r2,
        GameComponentStore<T3> r3)
    {
        public readonly GameEntityId Entity = e;
        public readonly int Index = idx;
        public ref T1 Value1 => ref r1.ByIndex(Index);
        public ref T2 Value2 => ref r2.ByIndex(Index);
        public ref T3 Value3 => ref r3.ByIndex(Index);
    }

    public EntityEnumerator<T1, T2, T3> GetEnumerator() => this;
}