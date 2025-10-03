using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Data;

public sealed class UpdateFrameInfo
{
    public long FrameIndex { get; private set; } = -1;
    public float DeltaTime { get; private set; }

    public int GameTick { get; set; } = 0;
    
    public Size2D Viewport { get; private set; }
    public Size2D OutputSize { get; private set; }
    public Size2D PrevOutputSize { get; private set; }

    public UpdateInfo UpdateInfo => new (GameTick, Fps, DeltaTime);

    public float Fps =>  DeltaTime > 0 ? 1.0f / DeltaTime : 0.0f;

    internal BeginFrameStatus BeginFrame(float deltaTime, Size2D viewport, Size2D outputSize)
    {
        FrameIndex++;
        DeltaTime = deltaTime;
        OutputSize = outputSize;
        Viewport =  viewport;
        var status = BeginFrameStatus.None;
        if (PrevOutputSize != outputSize) status = BeginFrameStatus.Resize;
        return status;
    }
    
    internal enum BeginFrameStatus
    {
        None,
        Resize
    }
}