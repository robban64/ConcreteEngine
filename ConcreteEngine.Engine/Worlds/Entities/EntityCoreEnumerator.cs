#region

using ConcreteEngine.Engine.Worlds.Entities.Components;

#endregion

namespace ConcreteEngine.Engine.Worlds.Entities;

internal ref struct EntityCoreEnumerator(EntityCoreStore r)
{
    private int _i = -1;
    public bool MoveNext() => ++_i < r.Count;
    public EntityCoreQuery Current => new(_i, r);

    public EntityCoreEnumerator GetEnumerator()
    {
        _i = -1;
        return this;
    }

    internal readonly ref struct EntityCoreQuery(int idx, EntityCoreStore r)
    {
        public readonly int Index = idx;
        public EntityId Entity => r.GetEntityByIndex(Index);
        public ref RenderSourceComponent Source => ref r.GetSourceByIndex(Index);
        public ref Transform Transform => ref r.GetTransformByIndex(Index);
        public ref BoxComponent Box => ref r.GetBoxByIndex(Index);
    }
}