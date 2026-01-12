using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Graphics;

public readonly struct GfxFrameArgs(float deltaTime, Size2D outputSize)
{
    public readonly Size2D OutputSize = outputSize;
    public readonly float DeltaTime = deltaTime;
}