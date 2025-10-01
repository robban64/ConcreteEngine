#region

using ConcreteEngine.Common.Numerics;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics;

public readonly struct FrameInfo(
    long frameIndex,
    float deltaTime,
    bool vSyncEnabled,
    Bounds2D viewport,
    Bounds2D outputSize)
{
    public readonly long FrameIndex = frameIndex;
    public readonly float DeltaTime = deltaTime;
    public readonly Bounds2D Viewport = viewport;
    public readonly Bounds2D OutputSize = outputSize;
    public readonly bool vSyncEnabled = vSyncEnabled;
}



public readonly record struct GpuFrameStats(int DrawCalls, int TriangleCount);