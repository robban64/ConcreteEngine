#region

using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Data;

public struct GraphicsFrameContext
{
    public float DeltaTime;
    public float FramesPerSecond;
    public Vector2D<int> ViewportSize;
    public Vector2D<int> FramebufferSize;
}