using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Engine;

public static class EngineTime
{
    private static FastRandom _rng = new(12323);

    public static ulong FrameId;
    public static ulong GameTickId;
    public static float FrameRng;
    
    public static double Time;
    public static double DeltaTime;
    public static double Fps;

    public static double GameDelta;
    public static double GameAlpha;

    public static double SimulationDelta;
    public static double SimulationAlpha;
    
    public static float TimeF => (float)Time;
    public static float DeltaTimeF => (float)DeltaTime;
    public static float FpsF => (float)Fps;
    
    public static float GameDeltaF => (float)GameDelta;
    public static float GameAlphaF => (float)GameAlpha;
    
    public static float SimulationDeltaF => (float)SimulationDelta;
    public static float SimulationAlphaF => (float)SimulationAlpha;

    public const int SystemTickRate = 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void AdvanceFrame(double deltaTime, double gameAlpha, double simAlpha)
    {
        ++FrameId;
        DeltaTime = deltaTime;
        Time += deltaTime;
        Fps = deltaTime / (deltaTime * deltaTime + double.Epsilon);
        FrameRng = _rng.NextFloat();
        GameAlpha = gameAlpha;
        SimulationAlpha = simAlpha;
    }
}