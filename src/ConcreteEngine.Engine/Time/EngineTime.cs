namespace ConcreteEngine.Engine.Time;

public static class EngineTime
{
    public static long UpdateId;
    public static long FrameId;

    public static long Timestamp;

    public static float Time;

    public static float DeltaTime;

    public static float GameAlpha;
    public static float SimulationAlpha;

    public static float GameTickDeltaTime;
    public static float SimulationDeltaTime;
    public static float DiagnosticTickDeltaTime;

    public static float Fps => DeltaTime > 0 ? 1.0f / DeltaTime : 0.0f;
}