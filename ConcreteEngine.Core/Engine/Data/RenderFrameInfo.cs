using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Engine.Platform;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Engine.Data;

public sealed class RenderFrameInfo
{
    public long FrameIndex { get; private set; } = -1;
    public float Alpha { get; private set; } = 0;
    public float Time { get; private set; } = 0;

    public Size2D OutputSize { get; private set; }
    public Size2D PrevOutputSize { get; private set; }

    public GfxFrameResult GfxResult { get; private set; }

    private int RandomSeed => (int)FrameIndex + 666;

    internal BeginFrameStatus BeginRenderFrame(
        float dt, float alpha,
        IEngineWindowHost window,
        IEngineInputSource input,
        out RenderTickInfo tickInfo,
        out RenderTickParams tickParams)
    {
        FrameIndex++;
        Time += dt;
        Alpha = alpha;
        OutputSize = window.FramebufferSize;

        tickInfo = new RenderTickInfo(FrameIndex, dt, Alpha, OutputSize);
        tickParams = new RenderTickParams(window.Size, input.MousePosition, Time, 9999);

        var status = BeginFrameStatus.None;
        if (PrevOutputSize != OutputSize) status = BeginFrameStatus.Resize;
        return status;
    }


    internal void EndRenderFrame(GfxFrameResult gfxFrameResult)
    {
        PrevOutputSize = OutputSize;
        GfxResult = gfxFrameResult;
    }


    internal enum BeginFrameStatus
    {
        None,
        Resize
    }
}