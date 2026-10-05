using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Render.Components;
using static ConcreteEngine.Core.Engine.Render.RenderWorld.Query;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
    public static partial class Query
    {
        //TODO
        public ref struct FilterQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private int _entity;
            private readonly BitSet _filter;

            private readonly Span<T1> _data;
            private readonly ReadOnlySpan<ushort> _generations;

            public QueryItem<T1> Current { get; private set; }

            public FilterQuery(BitSet filter)
            {
                _entity = -1;
                _filter = filter;
                _generations = MetaStore.GenerationSpan();
                _data = Dense<T1>().AsSpan();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_entity < _data.Length)
                {
                    if (_filter[_entity])
                    {
                        Current = new QueryItem<T1>(new RenderEntity(_entity, _generations[_entity]), ref _data[_entity]);
                        return true;
                    }
                }

                return false;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterQuery<T1> GetEnumerator() => this;
        }
    }
}