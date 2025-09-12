#region

using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics;

public readonly struct FrameInfo(
    long frameIndex,
    bool vSyncEnabled,
    bool resizePending,
    Vector2D<int> viewport,
    Vector2D<int> outputSize)
{
    public readonly long FrameIndex = frameIndex;
    public readonly bool vSyncEnabled = vSyncEnabled;
    public readonly bool  ResizePending = resizePending;
    public readonly Vector2D<int> Viewport = viewport;
    public readonly Vector2D<int> OutputSize = outputSize;
}

public readonly record struct GpuFrameStats(int DrawCalls, int TriangleCount);