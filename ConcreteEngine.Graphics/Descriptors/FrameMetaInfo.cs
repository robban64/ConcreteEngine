#region

using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Descriptors;

public struct FrameMetaInfo
{
    public float DeltaTime;
    public float Fps;
    public Vector2D<int> ViewportSize;
    public Vector2D<int> FramebufferSize;
}

public readonly record struct FrameRenderResult(int DrawCalls, int TriangleCount);