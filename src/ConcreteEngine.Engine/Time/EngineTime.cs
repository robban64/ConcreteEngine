namespace ConcreteEngine.Engine.Time;

public static class EngineTime
{
    public static long FrameId;
    public static long GameTickId;

    public static long Timestamp;

    public static float Time;

    public static float DeltaTime;

    public static float GameAlpha;
    public static float EnvironmentAlpha;

    public static float GameTickDeltaTime;
    public static float EnvironmentDeltaTime;

    public static float Fps;
}