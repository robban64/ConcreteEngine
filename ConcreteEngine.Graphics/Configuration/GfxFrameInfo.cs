#region

using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Graphics;

public readonly struct GfxFrameInfo(
    long frameIndex,
    float deltaTime,
    Size2D outputSize)
{
    public readonly long FrameIndex = frameIndex;
    public readonly float DeltaTime = deltaTime;
    public readonly Size2D OutputSize = outputSize;
}

public readonly struct GfxFrameResult(int drawCalls, int triangleCount)
{
    public readonly int DrawCalls = drawCalls;
    public readonly int TriangleCount = triangleCount;
}