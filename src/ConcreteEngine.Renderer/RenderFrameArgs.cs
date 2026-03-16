using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Renderer;

public struct RenderFrameArgs
{
    public Size2D OutputSize;
    public Vector2 MousePos;
    public float DeltaTime;
    public float Time;
    public float Alpha;
    public float Rng;
}