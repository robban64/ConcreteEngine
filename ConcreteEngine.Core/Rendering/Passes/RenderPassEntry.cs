using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public interface IRenderTargetState
{
    public ShaderId? TargetShader { get; }
}

public class RenderTargetFsqState
{
    public List<TextureId> SourceTextureIds { get; }
    public RenderBufferId DepthBufferId { get; }
}

public class RenderTargetResolveState
{
    public FrameBufferId ResolveToId { get; }
    public bool LinearFilter { get; }
}

public delegate PassMutationState RenderPassMutate<TState>(in TState currentState)
    where TState : unmanaged, IRenderPassState<TState>;

public delegate ApplyPassReturn RenderPassOp<TState>(in RenderPassCtx ctx, in TState state)
    where TState : unmanaged, IRenderPassState<TState>;

public delegate void RenderAfterPassOp<TState>(in RenderPassCtx ctx, in TState state)
    where TState : unmanaged, IRenderPassState<TState>;

public interface IRenderPassEntry
{
    RenderTargetId TargetId { get; }
    int Index { get; }
    int FboSlot { get; }
    ApplyPassReturn ApplyPass(in RenderPassCtx ctx);
    void ApplyAfterPass(in RenderPassCtx ctx);
}

public sealed class RenderPassEntry<TState> : IRenderPassEntry where TState : unmanaged, IRenderPassState<TState>
{
    public RenderTargetId TargetId { get; }
    public int Index { get; }
    public int FboSlot { get; private set; } = 0; //0 = screen
    
    private RenderPassOp<TState>? _applyPassDel;
    private RenderAfterPassOp<TState>? _applyAfterPassDel;
    private RenderPassMutate<TState>? _applyPassMutateDel;

    private readonly TState _defaultState;
    private TState _state;

    private PassMutationState? _pendingState = null;


    internal RenderPassEntry(RenderTargetId targetId, int index, TState initial)
    {
        TargetId = targetId;
        Index = index;
        _defaultState = initial;
        _state = initial;
    }

    public RenderPassEntry<TState> WithFbo(int fboSlot)
    {
        FboSlot = fboSlot;
        return this;
    }

    public RenderPassEntry<TState> OnPassBegin(RenderPassOp<TState> op)
    {
        _applyPassDel = op;
        return this;
    }

    public RenderPassEntry<TState> OnPassEnd(RenderAfterPassOp<TState> op)
    {
        _applyAfterPassDel = op;
        return this;
    }

    public void UpdateState(RenderPassMutate<TState> mutate) => _applyPassMutateDel = mutate;

    public void UpdateState(in PassMutationState replace) => _pendingState = replace;


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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ApplyPassReturn ApplyPass(in RenderPassCtx ctx)
    {
        ApplyPending();
        
        if (_applyPassDel is { } applyPassDel)
            return applyPassDel(in ctx, in _state);

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyAfterPass(in RenderPassCtx ctx)
    {
        if (_applyAfterPassDel is { } afterPassDel)
            afterPassDel(in ctx, in _state);
    }

}
