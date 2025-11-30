#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Renderer.State;

public enum BeginFrameStatus
{
    None,
    Resize
}

public readonly struct RenderFrameInfo(long frameIndex, float deltaTime, float alpha, Size2D outputSize)
{
    public long FrameIndex { get; init; } = frameIndex;
    public float DeltaTime { get; init; } = deltaTime;
    public float Alpha { get; init; } = alpha;
    public Size2D OutputSize { get; init; } = outputSize;

    public float Fps => DeltaTime > 0 ? 1.0f / DeltaTime : 0.0f;

    public GfxFrameInfo ToGfxFrameInfo() => new(FrameIndex, DeltaTime, OutputSize);
}

public readonly struct RenderRuntimeParams(
    Size2D screenSize,
    Vector2 mousePos,
    float time,
    int rndSeed,
    float defaultRandom)
{
    public Size2D ScreenSize { get; init; } = screenSize;
    public Vector2 MousePos { get; init; } = mousePos;
    public float Time { get; init; } = time;
    public float DefaultRandom { get; init; } = defaultRandom;
    public int RndSeed { get; init; } = rndSeed;
}