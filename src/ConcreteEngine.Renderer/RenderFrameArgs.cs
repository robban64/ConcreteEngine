using System.Numerics;

namespace ConcreteEngine.Renderer;

public readonly struct RenderFrameArgs(Vector2 mousePos, float time, float rng)
{
    public readonly Vector2 MousePos = mousePos;
    public readonly float Time = time;
    public readonly float Rng = rng;
}