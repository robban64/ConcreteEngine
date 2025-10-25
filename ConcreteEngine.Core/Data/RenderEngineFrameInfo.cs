#region

using System.Diagnostics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Graphics;
using ConcreteEngine.Renderer.State;

#endregion

namespace ConcreteEngine.Core.Data;

public sealed class RenderEngineFrameInfo
{
    public long FrameIndex { get; private set; } = -1;
    public long TimeStamp { get; private set; } = -1;
    public float Alpha { get; private set; } = 0;
    public float Time { get; private set; } = 0;
    public float Fps { get; private set; } = 0;
    public Size2D OutputSize { get; private set; }
    public Size2D PrevOutputSize { get; private set; }

    public GfxFrameResult GfxResult { get; private set; }
    public ref readonly RenderFrameInfo GetRenderFrameInfo() => ref _renderFrameInfo;

    private RenderFrameInfo _renderFrameInfo;

    private int RandomSeed => (int)FrameIndex + 666;

    public RenderEngineFrameInfo(Size2D outputSize)
    {
        OutputSize = outputSize;
        PrevOutputSize = outputSize;
    }


    internal BeginFrameStatus BeginRenderFrame(
        float dt, float alpha,
        EngineWindow window,
        IEngineInputSource input,
        out RenderFrameInfo frameInfo,
        out RenderRuntimeParams runtimeParams)
    {
        TimeStamp = Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
        FrameIndex++;
        Time += dt;
        Alpha = alpha;
        Fps = dt > 0 ? 1.0f / dt : 0.0f;
        OutputSize = window.OutputSize;

        _renderFrameInfo = frameInfo = new RenderFrameInfo(FrameIndex, dt, Alpha, OutputSize);
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