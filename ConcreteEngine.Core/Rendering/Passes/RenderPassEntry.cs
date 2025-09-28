namespace ConcreteEngine.Core.Rendering;


public interface IRenderPassState;

public sealed class EmptyState : IRenderPassState
{
    private static EmptyState? _instance;
    public static EmptyState Instance => _instance ??= new EmptyState();
    private EmptyState(){}
}


public interface IRenderPassEntry
{
    void ExecuteAfter(in RenderPassCtx ctx);
    void ExecuteBefore(in RenderPassCtx ctx);
}



public readonly record struct RenderPassRef<TState> where TState : class, IRenderPassState;

public sealed class RenderPassEntry<TState> : IRenderPassEntry where TState : class, IRenderPassState
{
    public RenderTargetId TargetId { get; }
    public int Index { get; }
    public RenderPassOp<TState>? Before { get; private set; }
    public RenderPassOp<TState>? After  { get; private set; }
    
    private readonly TState _state;

    internal RenderPassEntry( RenderTargetId targetId, int index, TState initial)
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
        if (Before is { } before) before(in ctx, in _state);
    }
    
    public void ExecuteAfter(in RenderPassCtx ctx)
    {
        if (After is { } after) after(in ctx, in _state);
    }

}