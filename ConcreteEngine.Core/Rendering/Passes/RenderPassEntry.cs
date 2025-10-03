#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Rendering.Data;

#endregion

namespace ConcreteEngine.Core.Rendering.Passes;

public delegate PassMutationState RenderPassMutate(RenderPassState currentState);

public delegate ApplyPassReturn RenderPassOp(RenderPassCtx ctx, in RenderPassState state);

public delegate void RenderAfterPassOp(RenderPassCtx ctx, in RenderPassState state);

public sealed class RenderPassEntry
{
    public RenderTargetId TargetId { get; }
    public int PassIndex { get; }
    public PassTagKey TagKey { get; private set; }


    private RenderPassOp? _applyPassDel;
    private RenderAfterPassOp? _applyAfterPassDel;
    private RenderPassMutate? _applyPassMutateDel;

    private readonly RenderPassState _defaultState;
    private RenderPassState _state;

    private PassMutationState? _pendingState = null;


    internal RenderPassEntry(RenderTargetId targetId, PassTagKey tagKey, int passIndex, RenderPassState initial)
    {
        TargetId = targetId;
        TagKey = tagKey;
        PassIndex = passIndex;
        _defaultState = initial;
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


    public ApplyPassReturn ApplyPass(RenderPassCtx ctx)
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