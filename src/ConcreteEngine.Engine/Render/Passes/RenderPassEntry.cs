using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Passes;

internal sealed class RenderPassEntry : IComparable<PassTargetKey>
{
    private static PassAction NoOpPass(RenderPassContext ctx) => default;

    public readonly PassTargetKey PassKey;
    public readonly PassOp PassOp;

    public readonly RenderPassParams Params;
    public readonly GfxPassState GfxState;

    public Func<RenderPassContext, PassAction> BeginPassDel { get; private set; } = NoOpPass;
    public Action<RenderPassContext>? EndPassDel { get; private set; }

    internal RenderPassEntry(PassTargetKey key, PassOp op, GfxPassState gfxState, FrameBufferId target,
        ShaderId passShader, bool linearFilter)
    {
        PassKey = key;
        PassOp = op;
        GfxState = gfxState;
        Params.Target = target;
        Params.PassShader = passShader;
        Params.LinearFilter = linearFilter;
    }

    public RenderPassEntry OnPassBegin(Func<RenderPassContext, PassAction> op)
    {
        BeginPassDel = op;
        return this;
    }

    public RenderPassEntry OnPassEnd(Action<RenderPassContext> op)
    {
        EndPassDel = op;
        return this;
    }

    public int CompareTo(PassTargetKey other) => PassKey.CompareTo(other);
}