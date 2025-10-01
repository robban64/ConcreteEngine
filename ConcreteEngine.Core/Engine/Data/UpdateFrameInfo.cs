using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Engine;

public sealed class UpdateFrameInfo
{
    public long FrameIndex { get; private set; } = -1;
    public float Fps { get; private set; }
    public float DeltaTime { get; private set; }

    public int GameTick { get; set; } = 0;
    
    public Size2D Viewport { get; private set; }
    public Size2D OutputSize { get; private set; }
    public Size2D PrevOutputSize { get; private set; }

    public UpdateInfo UpdateInfo => new (GameTick, Fps, DeltaTime);

    internal BeginFrameStatus BeginFrame(float fps, float deltaTime, Size2D viewport, Size2D outputSize)
    {
        FrameIndex++;
        Fps = fps;
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