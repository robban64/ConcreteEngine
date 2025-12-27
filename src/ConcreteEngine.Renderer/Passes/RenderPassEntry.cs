using System.Runtime.CompilerServices;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Renderer.Passes;

public delegate PassAction RenderPassOp(RenderPassCtx ctx, in RenderPassState state);

public delegate void RenderAfterPassOp(RenderPassCtx ctx, in RenderPassState state);

public sealed class RenderPassEntry
{
    private static PassAction NoOpPass(RenderPassCtx ctx, in RenderPassState state) => default;
    private static void NoOpAfterPass(RenderPassCtx ctx, in RenderPassState state) { }

    public PassTagKey PassKey { get; private set; }
    public PassOpKind PassOp { get; private set; }
    public PassTagKey? DependsOn { get; }

    private RenderPassOp _applyPassDel = NoOpPass;
    private RenderAfterPassOp _applyAfterPassDel = NoOpAfterPass;

    private readonly RenderPassState _defaultState;
    private RenderPassState _state;

    private PassMutationState _pendingState;
    private bool _hasPending;


    internal RenderPassEntry(PassTagKey passKey, PassOpKind passOp, RenderPassState initial,
        PassTagKey? dependsOn = null)
    {
        PassKey = passKey;
        _defaultState = initial;
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
    public PassAction ApplyPass(RenderPassCtx ctx)
    {
        ApplyPending();
        return _applyPassDel(ctx, in _state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyAfterPass(RenderPassCtx ctx)
    {
        _applyAfterPassDel(ctx, in _state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyPending()
    {
        if (!_hasPending) return;

        _state = _state.FromMutation(in _pendingState);
        _pendingState = default;
        _hasPending = false;
    }
}