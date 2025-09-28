namespace ConcreteEngine.Core.Rendering;


public interface IRenderPassEntry
{
    public RenderTargetId TargetId { get; }
    public int Index { get; }

    void ExecuteAfter(in RenderPassCtx ctx);
    void ExecuteBefore(in RenderPassCtx ctx);
}

public sealed class RenderPassEntry<TState> : IRenderPassEntry where TState : IRenderPassState
{
    public RenderTargetId TargetId { get; }
    public int Index { get; }
    public RenderPassOp<TState>? Before { get; private set; }
    public RenderPassOp<TState>? After { get; private set; }

    private readonly TState _state;

    internal RenderPassEntry(RenderTargetId targetId, int index, TState initial)
    {
        TargetId = targetId;
        Index = index;
        _state = initial;
    }

    public RenderPassEntry<TState> AddBeforeOp(RenderPassOp<TState> op)
    {
        Before = op;
        return this;
    }

    public RenderPassEntry<TState> AddAfterOp(RenderPassOp<TState> op)
    {
        Before = op;
        return this;
    }

    public void ExecuteBefore(in RenderPassCtx ctx) => Before?.Invoke(in ctx, in _state);

    public void ExecuteAfter(in RenderPassCtx ctx) => After?.Invoke(in ctx, in _state);
}
/*
 
     public void MutateState(RenderPassMutate<TState> mutateAction)
   {
       mutateAction(in _state);
   }
if (_pendingMutate is { } mut)
{
    mut(_state);
    _pendingMutate = null;
}
private RenderPassMutate<TState>? _pendingMutate;

public void UpdateState(RenderPassMutate<TState> mutate) => _pendingMutate = mutate;
*/