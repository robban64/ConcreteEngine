using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Passes;

internal sealed class RenderPassEntry : IComparable<PassTargetKey>
{
    private static PassAction NoOpPass(RenderPassContext ctx, GfxPassState gfxState) => default;

    public PassTargetKey PassKey { get; }
    public PassOp PassOp { get; private set; }

    public PassState State;
    private InlineArray4<TextureId> _sources;
    private readonly GfxPassState _gfxState;

    private Func<RenderPassContext, GfxPassState, PassAction> _applyPassDel = NoOpPass;
    private Action<RenderPassContext, GfxPassState>? _applyAfterPassDel;

    internal RenderPassEntry(PassTargetKey key, PassOp op, GfxPassState gfxState, FrameBufferId target,
        ShaderId passShader, bool linearFilter)
    {
        PassKey = key;
        PassOp = op;
        _gfxState = gfxState;
        State.Target = target;
        State.PassShader = passShader;
        State.LinearFilter = linearFilter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InlineArray4<TextureId> GetSources() => _sources;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSourceSlot(byte slot, TextureId id) => _sources[slot] = id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PassAction ApplyPass(DrawCommandProcessor drawCmd)
    {
        return _applyPassDel(new RenderPassContext(this, drawCmd), _gfxState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyAfterPass(DrawCommandProcessor drawCmd)
    {
        _applyAfterPassDel?.Invoke(new RenderPassContext(this, drawCmd), _gfxState);
        _sources = default;
    }

    public RenderPassEntry OnPassBegin(Func<RenderPassContext, GfxPassState, PassAction> op)
    {
        _applyPassDel = op;
        return this;
    }

    public RenderPassEntry OnPassEnd(Action<RenderPassContext, GfxPassState> op)
    {
        _applyAfterPassDel = op;
        return this;
    }

    public int CompareTo(PassTargetKey other) => PassKey.CompareTo(other);
}