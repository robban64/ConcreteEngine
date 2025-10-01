using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Engine;

internal sealed class EngineFrameInfo
{
    private FrameInfo _frameInfo;

    public long FrameIndex { get; private set; } = -1;
    public UpdateInfo Update { get; private set; }
    public GpuFrameStats GpuStats { get; private set; }
    public Size2D PrevOutputSize { get; private set; }

    public ref readonly FrameInfo Frame => ref _frameInfo;

    public BeginFrameStatus BeginFrame(float fps, float deltaTime, Size2D viewport, Size2D outputSize)
    {
        FrameIndex++;
        Update = Update with {  Fps = fps, DeltaTime = deltaTime };
        _frameInfo = new FrameInfo(FrameIndex, deltaTime, true, viewport, outputSize);

        var status = BeginFrameStatus.None;
        if (PrevOutputSize != outputSize) status = BeginFrameStatus.Resize;
        PrevOutputSize = outputSize;

        return status;
    }
    

    public void EndFrame(GpuFrameStats stats) => GpuStats = stats;

    internal enum BeginFrameStatus
    {
        None,
        Resize
    }
}