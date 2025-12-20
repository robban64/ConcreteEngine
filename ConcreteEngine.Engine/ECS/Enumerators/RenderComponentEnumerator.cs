using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS.Enumerators;

internal ref struct RenderComponentEnumerator<T1>(RenderEntityStore<T1> r)
    where T1 : unmanaged, IRenderComponent<T1>
{
    private int _i = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < r.Count;

    public Item Current => new(_i, r);

    public readonly ref struct Item(int idx, RenderEntityStore<T1> r)
    {
        public readonly int Index = idx;
        public RenderEntityId RenderEntity => r.GetEntity(Index);
        public ref T1 Component => ref r.GetByIndex(Index);
    }

    public RenderComponentEnumerator<T1> GetEnumerator()
    {
        _i = -1;
        return this;
    }
}