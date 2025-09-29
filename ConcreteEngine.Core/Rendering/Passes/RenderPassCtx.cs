using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;


public sealed class RenderPassCtx
{
    public RenderCommandOps CmdOps { get; }
    public FrameBufferId FboId { get; private set; }
    public FrameBufferMeta Meta { get; private set; }
    public int Pass { get; internal set; } = 0;
    
    private readonly List<NextAction> _pushed = new(4);
    
    internal RenderPassCtx(RenderCommandOps cmdOps)
    {
        CmdOps = cmdOps;
    }

    public void PushNext(NextAction action) => _pushed.Add(action);
    public IReadOnlyList<NextAction> Pushed => _pushed;

    internal void FromBinding(RenderFbo fbo)
    {
        FboId = fbo.FboId;
        Meta = fbo.GetMeta();
        Pass = 0;
        _pushed.Clear();
    }
}