using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;


public sealed class RenderPassCtx
{
    public RenderCommandOps CmdOps { get; }
    public FrameBufferId FboId { get; private set; }
    public FrameBufferMeta Meta { get; private set; }
    public int Pass { get; internal set; } = 0;
    public ShaderId ScreenShader { get; private set; }
    
    private readonly List<PassReturn> _pushed = new(4);
    
    internal RenderPassCtx(RenderCommandOps cmdOps)
    {
        CmdOps = cmdOps;
    }

    public void BindScreenShader(ShaderId shader) => ScreenShader = shader;

    public void PushNext(PassReturn action) => _pushed.Add(action);
    public IReadOnlyList<PassReturn> Pushed => _pushed;

    internal void FromBinding(RenderFbo fbo)
    {
        FboId = fbo.FboId;
        Meta = fbo.GetMeta();
        Pass = 0;
        _pushed.Clear();
    }
}