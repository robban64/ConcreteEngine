using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Passes;

internal sealed class RenderPassEntry : IComparable<PassTargetKey>
{
    private static PassAction NoOpPass(RenderPassContext ctx) => default;

    public readonly PassTargetKey PassKey;
    public readonly PassOp PassOp;

    public readonly PassState State;
    public readonly GfxPassState GfxState;

    public Func<RenderPassContext, PassAction> ApplyPassDel { get; private set; } = NoOpPass;
    public Action<RenderPassContext>? ApplyAfterPassDel { get; private set; }

    internal RenderPassEntry(PassTargetKey key, PassOp op, GfxPassState gfxState, FrameBufferId target,
        ShaderId passShader, bool linearFilter)
    {
        PassKey = key;
        PassOp = op;
        GfxState = gfxState;
        State.Target = target;
        State.PassShader = passShader;
        State.LinearFilter = linearFilter;
    }

    public RenderPassEntry OnPassBegin(Func<RenderPassContext, PassAction> op)
    {
        ApplyPassDel = op;
        return this;
    }

    public RenderPassEntry OnPassEnd(Action<RenderPassContext> op)
    {
        ApplyAfterPassDel = op;
        return this;
    }

    public int CompareTo(PassTargetKey other) => PassKey.CompareTo(other);
}