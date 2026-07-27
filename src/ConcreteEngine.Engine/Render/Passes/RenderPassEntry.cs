using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Render.Passes;

internal delegate PassAction RenderPassOp(RenderPassContext ctx, in RenderPassState state);
internal delegate void RenderAfterPassOp(RenderPassContext ctx, in RenderPassState state);

internal sealed class RenderPassEntry
{
    private static PassAction NoOpPass(RenderPassContext ctx, in RenderPassState state) => default;

    public PassTargetKey PassKey { get; private set; }
    public PassOp PassOp { get; private set; }
    public PassTargetKey? DependsOn { get; }

    private RenderPassOp _applyPassDel = NoOpPass;
    private RenderAfterPassOp? _applyAfterPassDel;

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
        return _applyPassDel(ctx, in _state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyAfterPass(RenderPassContext ctx) => _applyAfterPassDel?.Invoke(ctx, in _state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyPending()
    {
        if (!_hasPending) return;

        _state = _state.FromMutation(in _pendingState);
        _pendingState = default;
        _hasPending = false;
    }
}