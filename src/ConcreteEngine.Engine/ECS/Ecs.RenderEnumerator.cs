using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.ECS;
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

                public ref Transform Transform
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref Render.Core.GetTransform(RenderEntity);
                }

                public ref BoundingBox Box
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref Render.Core.GetBox(RenderEntity);
                }

                public ref Matrix4x4 Parent
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref Render.Core.GetParentMatrix(RenderEntity);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public TuplePtr<Transform, BoundingBox> TryGetSpatial() =>
                    Render.Core.TryGetSpatial(RenderEntity);
            }
        }
    }

    public static class RenderQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
    {
        public ref struct RenderEntityEnumerator()
        {
            private int _i = -1;
            private RenderEntityId _currentEntity;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < Render.Stores<T1>.Store.Count)
                {
                    var entity = Render.Stores<T1>.Store.GetEntity(_i);
                    if (entity.IsValid())
                    {
                        _currentEntity = entity;
                        return true;
                    }
                }

                return false;
            }


            public readonly Item Current => new(_i, _currentEntity);

            public readonly ref struct Item(int idx, RenderEntityId entityId)
            {
                public readonly int Index = idx;
                public readonly RenderEntityId RenderEntity = entityId;

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