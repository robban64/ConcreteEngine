#region

using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics;

public readonly struct FrameInfo(
    long frameIndex,
    float deltaTime,
    bool vSyncEnabled,
    bool resizePending,
    Vector2D<int> viewport,
    Vector2D<int> outputSize)
{
    public readonly long FrameIndex = frameIndex;
    public readonly float DeltaTime = deltaTime;
    public readonly Vector2D<int> Viewport = viewport;
    public readonly Vector2D<int> OutputSize = outputSize;
    public readonly bool vSyncEnabled = vSyncEnabled;
    public readonly bool ResizePending = resizePending;
}

public readonly record struct GpuFrameStats(int DrawCalls, int TriangleCount);