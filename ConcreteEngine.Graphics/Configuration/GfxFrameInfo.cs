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

public readonly record struct GfxFrameResult(int DrawCalls, int TriangleCount);