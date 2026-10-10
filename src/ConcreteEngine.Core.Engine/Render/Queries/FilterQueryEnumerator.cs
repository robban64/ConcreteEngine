using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Render;

public interface IBitOp
{
    static abstract Bit256 Apply(Bit256 left, Bit256 right);
}

public interface IBitDataOp<in T> where T : struct, IBitDataOp<T>, allows ref struct
{
    static abstract Bit256 Apply(T it, int index);
}

public struct OpAnd : IBitOp
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bit256 Apply(Bit256 left, Bit256 right) => Bit256.And(left, right);
}

public struct OpAndNot : IBitOp
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bit256 Apply(Bit256 left, Bit256 right) => Bit256.And(left, right);
}

public readonly ref struct FilterSet<TOp>(BitSet filter1, BitSet filter2) : IBitDataOp<FilterSet<TOp>>
    where TOp : struct, IBitOp
{
    private readonly BitSet _filter1 = filter1;
    private readonly BitSet _filter2 = filter2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bit256 Apply(FilterSet<TOp> it, int index)
    {
        var b1 = it._filter1.Get256(index);
        var b2 = it._filter2.Get256(index);
        return TOp.Apply(b1, b2);
    }
}

public readonly ref struct FilterSet<TOp, TOp2>(BitSet filter1, BitSet filter2, BitSet filter3) : IBitDataOp<FilterSet<TOp,TOp2>>
    where TOp : struct, IBitOp where TOp2 : struct, IBitOp
{
    private readonly BitSet _filter1 = filter1;
    private readonly BitSet _filter2 = filter2;
    private readonly BitSet _filter3 = filter3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bit256 Apply(FilterSet<TOp,TOp2> it, int index)
    {
        var b1 = it._filter1.Get256(index);
        var b2 = it._filter2.Get256(index);
        var b3 = it._filter3.Get256(index);
        return TOp2.Apply(TOp.Apply(b1, b2), b3);
    }
}


public readonly ref struct FilterSetRight<TOp>(BitSet filter1, Bit256 right) 
    : IBitDataOp<FilterSetRight<TOp>> where TOp : struct, IBitOp
{
    private readonly BitSet _filter1 = filter1;
    private readonly Bit256 _right = right;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bit256 Apply(FilterSetRight<TOp> it, int index)
    {
        var b1 = it._filter1.Get256(index);
        return TOp.Apply(b1, it._right);
    }
}

public readonly ref struct FilterQueryItem(int blockCount, int blockIdx, Bit256 chunk)
{
    public readonly int BlockCount = blockCount;
    public readonly int BlockIdx = blockIdx;
    public readonly Bit256 Chunk = chunk;
    
    public int Lanes => int.Min(BlockCount - BlockIdx, 4);

    public delegate void QueryAction<TContext>(ref TContext ctx, int lane, int start, Bit64 block)
        where TContext : struct, allows ref struct;

    public void ForEachLane<TContext>(ref TContext ctx, QueryAction<TContext> action)
        where TContext : struct, allows ref struct
    {
        var lanes = Lanes;
        for (int lane = 0; lane < lanes; lane++)
        {
            var block = Chunk.GetBit64(lane);
            if (block.IsAnySet)
            {
                var start = (BlockIdx + lane) << 6;
                action(ref ctx, lane, start, block);
            }
        }
    }
}

public ref struct FilterQueryEnumerator<TOp> where TOp : struct, IBitDataOp<TOp>, allows ref struct
{
    private readonly int _chunkCount;
    private int _chunkCursor;
    private Bit256 _currentChunk;

    private readonly TOp _filter;

    public FilterQueryEnumerator(int chunkCount, TOp filter)
    {
        _chunkCount = chunkCount;
        _chunkCursor = -1;
        _currentChunk = Bit256.Zero;
        _filter = filter;
    }

    public readonly FilterQueryItem Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_chunkCount, _chunkCursor << 2, _currentChunk);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        while (++_chunkCursor < _chunkCount)
        {
            _currentChunk = TOp.Apply(_filter, _chunkCursor << 2);
            if (_currentChunk.IsAnySet) return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly FilterQueryEnumerator<TOp> GetEnumerator() => this;
}

