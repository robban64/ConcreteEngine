using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Renderer.State;

public enum BeginFrameStatus
{
    None,
    Resize
}

public readonly struct FrameInfo(long frameId, float deltaTime, float alpha, Size2D outputSize)
{
    public readonly long FrameId = frameId;
    public readonly Size2D OutputSize = outputSize;
    public readonly float DeltaTime = deltaTime;
    public readonly float Alpha = alpha;
}

public readonly struct RenderRuntimeParams(Size2D screenSize, Vector2 mousePos, float time, float rng)
{
    public readonly Size2D ScreenSize = screenSize;
    public readonly Vector2 MousePos = mousePos;
    public readonly float Time = time;
    public readonly float Rng = rng;
}