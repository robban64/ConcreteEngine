#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Core.Rendering.Passes;

public delegate PassMutationState RenderPassMutate(RenderPassState currentState);

public delegate PassAction RenderPassOp(RenderPassCtx ctx, in RenderPassState state);

public delegate void RenderAfterPassOp(RenderPassCtx ctx, in RenderPassState state);

public sealed class RenderPassEntry
{
    public PassTagKey PassKey { get; private set; }
    public PassOpKind PassOp { get; private set; }


    private RenderPassOp? _applyPassDel;
    private RenderAfterPassOp? _applyAfterPassDel;
    private RenderPassMutate? _applyPassMutateDel;

    private readonly RenderPassState _defaultState;
    private RenderPassState _state;

    private PassMutationState? _pendingState = null;


    internal RenderPassEntry(PassTagKey passKey, PassOpKind passOp, RenderPassState initial)
    {
        PassKey = passKey;
        _defaultState = initial;
        PassOp = passOp;
        _state = initial;
    }

    public RenderPassEntry OnPassBegin(RenderPassOp op)
    {
        _applyPassDel = op;
        return this;
    }

    public RenderPassEntry OnPassEnd(RenderAfterPassOp op)
    {
        _applyAfterPassDel = op;
        return this;
    }

    public void UpdateState(in RenderPassMutate mutate) => _applyPassMutateDel = mutate;

    public void UpdateState(in PassMutationState replace) => _pendingState = replace;


    public PassAction ApplyPass(RenderPassCtx ctx)
    {
        ApplyPending();

        if (_applyPassDel is { } applyPassDel)
            return applyPassDel(ctx, in _state);

        return default;
    }

    public void ApplyAfterPass(RenderPassCtx ctx)
    {
        if (_applyAfterPassDel is { } afterPassDel)
            afterPassDel(ctx, in _state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyPending()
    {
        if (_pendingState is { } rep)
        {
            _state = _state.FromMutation(in rep);
            _pendingState = null;
            _applyPassMutateDel = null;
            return;
        }

        if (_applyPassMutateDel is { } mut)
        {
            var mutationState = mut(_state);
            _state = _state.FromMutation(in mutationState);
            _applyPassMutateDel = null;
        }
    }
}