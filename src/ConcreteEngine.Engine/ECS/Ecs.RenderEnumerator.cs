using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS;

public static partial class Ecs
{
    public static class RenderQuery
    {
        public ref struct RenderEntityEnumerator()
        {
            private int _i = -1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_i < Render.Core.Count;

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

                public ref SourceComponent Source
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref Render.Core.GetSource(RenderEntity);
                }

                public ref RenderTransform Transform
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref Render.Core.GetTransform(RenderEntity);
                }

                public ref BoxComponent Box
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref Render.Core.GetBox(RenderEntity);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public TuplePtr<RenderTransform, BoxComponent> TryGetSpatial() =>
                    Render.Core.TryGetSpatial(RenderEntity);
            }
        }
    }

    public static class RenderQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
    {
        public ref struct RenderEntityEnumerator()
        {
            private int _i = -1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_i < Render.Stores<T1>.Store.Count;

            public readonly Item Current => new(_i);

            public readonly ref struct Item(int idx)
            {
                private readonly int Index = idx;

                public RenderEntityId RenderEntity
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => Render.Stores<T1>.Store.GetEntity(Index);
                }

                public ref T1 Component
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref Render.Stores<T1>.Store.GetByIndex(Index);
                }
            }

            public RenderEntityEnumerator GetEnumerator()
            {
                _i = -1;
                return this;
            }
        }
    }
}