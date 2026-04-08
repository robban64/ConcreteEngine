using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Renderer;

public readonly struct RenderFrameArgs(Vector2 invOutputSize, Vector2 mousePosUv, float deltaTime, float time, float alpha, float rng)
{
    public readonly Vector2 InvOutputSize = invOutputSize;
    public readonly Vector2 MousePosUv = mousePosUv;
    public readonly float DeltaTime = deltaTime;
    public readonly float Time = time;
    public readonly float Alpha = alpha;
    public readonly float Rng = rng;
}