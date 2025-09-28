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

    private RenderPassMutate<TState>? _pendingMutate;

    private readonly TState _state;

    public void UpdateState(RenderPassMutate<TState> mutate) => _pendingMutate = mutate;


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

    public void MutateState(RenderPassMutate<TState> mutateAction)
    {
        mutateAction(in _state);
    }

    public void ExecuteBefore(in RenderPassCtx ctx)
    {
        if (_pendingMutate is { } mut)
        {
            mut(_state);
            _pendingMutate = null;
        }

        Before?.Invoke(in ctx, in _state);
    }

    public void ExecuteAfter(in RenderPassCtx ctx)
    {
        After?.Invoke(in ctx, in _state);
    }
}