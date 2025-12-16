using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Worlds.Entities.Resources;

internal ref struct EntityEnumerator<T1>(EntityStore<T1> r)
    where T1 : unmanaged
{
    private int _i = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < r.Count;
    
    public Item Current => new (_i, r);

    public readonly ref struct Item(int idx, EntityStore<T1> r)
    {
        public readonly int Index = idx;
        public EntityId Entity => r.GetHandle(Index);
        public ref T1 Component => ref r.GetByIndex(Index);
    }

    public EntityEnumerator<T1> GetEnumerator()
    {
        _i = -1;
        return this;
    }
}
/*
internal struct EntityEnumerator<T1, T2>(EntityStore<T1> r1, EntityStore<T2> r2)
    where T1 : unmanaged where T2 : unmanaged
{
    private int _i = -1;

    public bool MoveNext() => ++_i < r1.Count;
    public Item Current => new Item(r1.GetEntityId(_i), _i, r1, r2);

    public readonly ref struct Item(EntityId e, int idx, EntityStore<T1> r1, EntityStore<T2> r2)
    {
        public readonly EntityId Entity = e;
        public readonly int Index = idx;
        public ref T1 Component1 => ref r1.GetByIndex(Index);
        public ref T2 Component2 => ref r2.GetByIndex(Index);
    }

    public EntityEnumerator<T1, T2> GetEnumerator()
    {
        _i = -1;
        return this;
    }
}

internal struct EntityEnumerator<T1, T2, T3>(EntityStore<T1> r1, EntityStore<T2> r2, EntityStore<T3> r3)
    where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
{
    private int _i = -1;

    public bool MoveNext() => ++_i < r1.Count;
    public Item Current => new Item(r1.GetEntityId(_i), _i, r1, r2, r3);

    public readonly ref struct Item(EntityId e, int idx, EntityStore<T1> r1, EntityStore<T2> r2, EntityStore<T3> r3)
    {
        public readonly EntityId Entity = e;
        public readonly int Index = idx;
        public ref T1 Component1 => ref r1.GetByIndex(Index);
        public ref T2 Component2 => ref r2.GetByIndex(Index);
        public ref T3 Component3 => ref r3.GetByIndex(Index);
    }

    public EntityEnumerator<T1, T2, T3> GetEnumerator()
    {
        _i = -1;
        return this;
    }
}*/