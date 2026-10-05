using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using static ConcreteEngine.Core.Engine.Render.RenderWorld.Queries;

namespace ConcreteEngine.Core.Engine.Render;

public ref struct BitSetCoreEnumerator<T1> where T1 : unmanaged
{
    private int _entity;
    private readonly BitSet _filter;

    private readonly Span<T1> _data;
    private readonly ReadOnlySpan<ushort> _generations;

    public QueryItem<T1> Current { get; private set; }

    public BitSetCoreEnumerator(BitSet filter, Span<T1> data, ReadOnlySpan<ushort> generations)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(data.Length, generations.Length);
        _entity = -1;
        _filter = filter;
        _generations = generations;
        _data = data;
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
    public readonly BitSetCoreEnumerator<T1> GetEnumerator() => this;
}