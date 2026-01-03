using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Engine.Time;

public static class EngineTime
{
    private static FastRandom _rng  = new(12323);

    public static long FrameId;
    public static long GameTickId;

    public static float Time;

    public static float DeltaTime;

    public static float GameAlpha;
    public static float EnvironmentAlpha;

    public static float GameDelta;
    public static float EnvironmentDelta;

    public static int SystemTickRate = 1;

    public static float Fps;

    public static float FrameRng;

    internal static void AdvanceFrame(float deltaTime, float gameAlpha, float envAlpha)
    {
        FrameId++;
        DeltaTime = deltaTime;
        Time += deltaTime;
        Fps = deltaTime / (deltaTime * deltaTime + FloatMath.SingularEpsilon);
        FrameRng = _rng.NextFloat();
        GameAlpha  = gameAlpha;
        EnvironmentAlpha = envAlpha;
    }

}