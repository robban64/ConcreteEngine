using System.Numerics;

namespace ConcreteEngine.Renderer;

public struct RenderFrameArgs(
    Vector2 invOutputSize,
    Vector2 mousePosUv,
    float deltaTime,
    float time,
    float rng)
{
    public Vector2 InvOutputSize = invOutputSize;
    public Vector2 MousePosUv = mousePosUv;
    public float DeltaTime = deltaTime;
    public float Time = time;
    public float Rng = rng;
}