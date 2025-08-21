using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Rendering.Experimental;

sealed class CommandLeaf<T> where T : unmanaged
{
    public required T[] Data;
    public required DrawCommandMeta[] Meta;

    public int Count;
    public int Capacity;
}

sealed class TargetNode<T> where T : unmanaged
{
    // fixed array indexed by CommandId
    public readonly CommandLeaf<T>?[] Cmds;
    public TargetNode(int cmdCount) => Cmds = new CommandLeaf<T>?[cmdCount];
}

public class TreeSubmitter<T> where T : unmanaged
{
    private readonly TargetNode<T>?[] _targets;

    public TreeSubmitter()
    {
        _targets = new TargetNode<T>?[RenderConsts.RenderTargetCount];
    }

    // --- Registration (non‑hot) ---
    public void Register(RenderTargetId t, DrawCommandId c, int capacity)
    {
        int ti = (int)t, ci = (int)c;


        var node = _targets[ti] ??= new TargetNode<T>(RenderConsts.DrawCommandTypeCount);
        var leaf = node.Cmds[ci];
        if (leaf is not null)
            throw new InvalidOperationException("Already registered.");

        node.Cmds[ci] = new CommandLeaf<T>
        {
            Data = new T[capacity],
            Meta = new DrawCommandMeta[capacity],
            Capacity = capacity, Count = 0
        };
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitDraw(in T cmd, in DrawCommandMeta meta)
    {
        var leaf = _targets[(int)meta.Target]!.Cmds[(int)meta.Id]!;
        int idx = leaf.Count;
        leaf.Data[idx] = cmd;
        leaf.Meta[idx] = meta;
        leaf.Count++;
    }

    // --- Hot path submit (unchecked capacity by contract) ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T EnqueueUnchecked(RenderTargetId t, DrawCommandId c)
    {
        var leaf = _targets[(int)t]!.Cmds[(int)c]!;
        int idx = leaf.Count;
        leaf.Count = idx + 1; // no overflow checks
        return ref leaf.Data[idx];
    }

    // --- Drain a single leaf as a Span ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> GetCommandQueue(RenderTargetId t, DrawCommandId c)
    {
        var leaf = _targets[(int)t]!.Cmds[(int)c]!;
        return leaf.Data.AsSpan(0, leaf.Count);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<DrawCommandMeta> GetMetaQueue(RenderTargetId t, DrawCommandId c)
    {
        var leaf = _targets[(int)t]!.Cmds[(int)c]!;
        return leaf.Meta.AsSpan(0, leaf.Count);
    }

    public void Reset(RenderTargetId t, DrawCommandId c)
    {
        var leaf = _targets[(int)t]?.Cmds[(int)c];
        if(leaf is not null)
            leaf.Count = 0;
    }
}