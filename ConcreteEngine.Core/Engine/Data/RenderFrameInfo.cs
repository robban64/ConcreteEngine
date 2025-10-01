using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Engine;

public sealed class RenderFrameInfo
{
    public long FrameIndex { get; private set; } = -1;
    public float Fps { get; private set; }
    public float DeltaTime { get; private set; }

    public float Alpha { get; set; } = 0;
    
    public Size2D Viewport { get; private set; }
    public Size2D OutputSize { get; private set; }
    public Size2D PrevOutputSize { get; private set; }
    
    public GfxFrameResult GfxResult { get; private set; }

    public GfxFrameInfo Frame => new(FrameIndex, DeltaTime, Fps, Viewport, OutputSize);

    internal BeginRenderFrameStatus BeginFrame(float fps, float deltaTime, float alpha, Size2D viewport, Size2D outputSize)
    {
        FrameIndex++;
        Fps = fps;
        DeltaTime = deltaTime;
        Alpha = alpha;
        OutputSize = outputSize;
        Viewport =  viewport;
        var status = BeginRenderFrameStatus.None;
        if (PrevOutputSize != outputSize) status = BeginRenderFrameStatus.Resize;
        return status;
    }
    
    internal void EndFrame(GfxFrameResult gfxFrameResult)
    {
        PrevOutputSize = Frame.OutputSize;
        GfxResult = gfxFrameResult;
    }

    internal enum BeginRenderFrameStatus
    {
        None,
        Resize
    }
}