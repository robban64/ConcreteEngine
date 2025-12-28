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

    public static float GameDelta;
    public static float EnvironmentDelta;

    public static int SystemTickRate = 1;

    public static float Fps;
}