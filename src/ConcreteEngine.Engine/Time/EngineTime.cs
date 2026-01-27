using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Engine.Time;

public static class EngineTime
{
    private static FastRandom _rng = new(12323);

    public static long FrameId;
    public static long GameTickId;

    public static float Time;

    public static float DeltaTime;

    public static float GameAlpha;
    public static float EnvironmentAlpha;

    public static float GameDelta;
    public static float EnvironmentDelta;

    public static readonly int SystemTickRate = 1;

    public static float Fps;

    public static float FrameRng;

    internal static RenderFrameArgs  MakeFrameArgs(Size2D outputSize, Vector2 mousePos)
    {
        return new RenderFrameArgs
        {
            Alpha = GameAlpha,
            DeltaTime = DeltaTime,
            Rng = FrameRng,
            Time = Time,
            MousePos = mousePos,
            OutputSize = outputSize,
        };
    }

    internal static void AdvanceFrame(float deltaTime, float gameAlpha, float envAlpha)
    {
        FrameId++;
        DeltaTime = deltaTime;
        Time += deltaTime;
        Fps = deltaTime / (deltaTime * deltaTime + FloatMath.SingularEpsilon);
        FrameRng = _rng.NextFloat();
        GameAlpha = gameAlpha;
        EnvironmentAlpha = envAlpha;
    }
}