namespace ConcreteEngine.Core.Rendering;

public interface IRenderPassEntry
{
    RenderTargetId TargetId { get; }
    int Index { get; }
    int FboSlot { get;  }
    PassReturn ExecuteAfter(in RenderPassCtx ctx);
    PassReturn ExecuteBefore(in RenderPassCtx ctx);
}

public sealed class RenderPassEntry<TState> : IRenderPassEntry where TState : IRenderPassState
{
    public RenderTargetId TargetId { get; }
    public int Index { get; }
    public int FboSlot { get; private set; } = 0; //0 = screen
    public RenderPassOp<TState>? Before { get; private set; }
    public RenderPassOp<TState>? After { get; private set; }

    private TState _state;


    internal RenderPassEntry(RenderTargetId targetId, int index, TState initial)
    {
        TargetId = targetId;
        Index = index;
        _state = initial;
    }
    
    public RenderPassEntry<TState> WithFbo(int fboSlot)
    {
        FboSlot = fboSlot;
        return this;
    }

    public RenderPassEntry<TState> AddBeforeOp(RenderPassOp<TState> op)
    {
        Before = op;
        return this;
    }

    public RenderPassEntry<TState> AddAfterOp(RenderPassOp<TState> op)
    {
        After = op;
        return this;
    }

    public PassReturn ExecuteBefore(in RenderPassCtx ctx) =>
        Before is null ? PassReturn.None : Before(in ctx, in _state);

    public PassReturn ExecuteAfter(in RenderPassCtx ctx) => After is null ? PassReturn.None : After(in ctx, in _state);
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