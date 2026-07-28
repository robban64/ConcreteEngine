using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Render.Passes;


internal sealed class RenderPassEntry
{
    private static PassAction NoOpPass(RenderPassContext ctx, RenderPassState state) => default;

    public PassTargetKey PassKey { get; private set; }
    public PassOp PassOp { get; private set; }
    public PassTargetKey? DependsOn { get; }

    private Func<RenderPassContext, RenderPassState, PassAction> _applyPassDel = NoOpPass;
    private Action<RenderPassContext, RenderPassState>? _applyAfterPassDel;

    private RenderPassState _state;

    private PassMutationState _pendingState;
    private bool _hasPending;


    internal RenderPassEntry(PassTargetKey passKey, PassOp passOp, RenderPassState initial,
        PassTargetKey? dependsOn = null)
    {
        PassKey = passKey;
        PassOp = passOp;
        DependsOn = dependsOn;
        _state = initial;
    }

    public RenderPassEntry OnPassBegin(Func<RenderPassContext, RenderPassState, PassAction> op)
    {
        _applyPassDel = op;
        return this;
    }

    public RenderPassEntry OnPassEnd(Action<RenderPassContext, RenderPassState> op)
    {
        _applyAfterPassDel = op;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateState(in PassMutationState replace)
    {
        _pendingState = replace;
        _hasPending = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PassAction ApplyPass(RenderPassContext ctx)
    {
        ApplyPending();
        return _applyPassDel(ctx,  _state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyAfterPass(RenderPassContext ctx) => _applyAfterPassDel?.Invoke(ctx,  _state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyPending()
    {
        if (!_hasPending) return;

        _state = _state.FromMutation(in _pendingState);
        _pendingState = default;
        _hasPending = false;
    }
}