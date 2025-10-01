#region

using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Graphics;

public readonly struct FrameInfo(
    long frameIndex,
    float deltaTime,
    bool vSyncEnabled,
    Size2D viewport,
    Size2D outputSize)
{
    public readonly long FrameIndex = frameIndex;
    public readonly float DeltaTime = deltaTime;
    public readonly Size2D Viewport = viewport;
    public readonly Size2D OutputSize = outputSize;
    public readonly bool vSyncEnabled = vSyncEnabled;
}

public readonly record struct GpuFrameStats(int DrawCalls, int TriangleCount);