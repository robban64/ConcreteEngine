using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Engine.Platform;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.State;
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
        out Rendering.State.RenderFrameInfo frameInfo,
        out RenderRuntimeParams runtimeParams)
    {
        FrameIndex++;
        Time += dt;
        Alpha = alpha;
        OutputSize = window.OutputSize;

        frameInfo = new Rendering.State.RenderFrameInfo(FrameIndex, dt, Alpha, OutputSize);
        runtimeParams = new RenderRuntimeParams(window.WindowSize, input.MousePosition, Time, 9999);

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