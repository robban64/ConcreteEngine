using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Graphics;

public readonly struct GfxFrameArgs(long frameId, float deltaTime, Size2D outputSize)
{
    public readonly long FrameId = frameId;
    public readonly float DeltaTime = deltaTime;
    public readonly Size2D OutputSize = outputSize;
}