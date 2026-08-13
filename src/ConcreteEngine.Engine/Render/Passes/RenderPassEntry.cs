using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Passes;

internal sealed class RenderPassEntry : IComparable<PassTargetKey>
{
    private static PassAction NoOpPass(RenderPassProgram ctx, GfxPassState state) => default;

    public PassTargetKey PassKey { get; }
    public PassOp PassOp { get; private set; }

    public RenderPassState State;
    private GfxPassState _gfxPassState;
    
    private readonly TextureId[] _sources = new TextureId[8];

    private Func<RenderPassProgram, GfxPassState, PassAction> _applyPassDel = NoOpPass;
    private Action<RenderPassProgram, GfxPassState>? _applyAfterPassDel;

    internal RenderPassEntry(PassTargetKey key, PassOp op, GfxPassState gfxState, ShaderId passShader, bool linearFilter)
    {
        PassKey = key;
        PassOp = op;
        _gfxPassState = gfxState;
        State.PassShader = passShader;
        State.LinearFilter = linearFilter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<TextureId> GetSources() => _sources;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSourceSlot(byte slot, TextureId id) => _sources[slot] = id;
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PassAction ApplyPass(RenderPassProgram ctx)
    {
        return _applyPassDel(ctx, _gfxPassState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyAfterPass(RenderPassProgram ctx)
    {
        _applyAfterPassDel?.Invoke(ctx, _gfxPassState);
        Array.Clear(_sources);
    }

    public RenderPassEntry OnPassBegin(Func<RenderPassProgram, GfxPassState, PassAction> op)
    {
        _applyPassDel = op;
        return this;
    }

    public RenderPassEntry OnPassEnd(Action<RenderPassProgram, GfxPassState> op)
    {
        _applyAfterPassDel = op;
        return this;
    }

    public int CompareTo(PassTargetKey other) => PassKey.CompareTo(other);
}