using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Generics;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS;

public static class RenderQuery
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RenderEntityEnumerator CoreQuery() => new();

    public ref struct RenderEntityEnumerator()
    {
        private int _i = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_i < GenericStore.CoreStore.Count;

        public readonly EntityCoreQuery Current => new(_i);

        public RenderEntityEnumerator GetEnumerator()
        {
            _i = -1;
            return this;
        }

        public readonly ref struct EntityCoreQuery(int idx)
        {
            public readonly int Index = idx;
            public readonly RenderEntityId RenderEntity = new(idx + 1);

            public int Count => GenericStore.CoreStore.Count;

            public ref SourceComponent Source
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref GenericStore.CoreStore.GetSource(RenderEntity);
            }

            public ref RenderTransform Transform
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref GenericStore.CoreStore.GetTransform(RenderEntity);
            }

            public ref BoxComponent Box
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref GenericStore.CoreStore.GetBox(RenderEntity);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TuplePtr<RenderTransform, BoxComponent> TryGetSpatial()
                => GenericStore.CoreStore.TryGetSpatial(RenderEntity);
        }
    }
}

public static class RenderQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RenderComponentEnumerator Query() => new();

    public ref struct RenderComponentEnumerator()
    {
        private int _i = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_i < GenericStore.Render<T1>.Store.Count;

        public Item Current => new(_i);

        public readonly ref struct Item(int idx)
        {
            public readonly int Index = idx;
            public RenderEntityId RenderEntity => GenericStore.Render<T1>.Store.GetEntity(Index);
            public ref T1 Component => ref GenericStore.Render<T1>.Store.GetByIndex(Index);
        }

        public RenderComponentEnumerator GetEnumerator()
        {
            _i = -1;
            return this;
        }
    }
}