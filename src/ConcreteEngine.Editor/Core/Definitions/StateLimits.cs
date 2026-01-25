namespace ConcreteEngine.Editor.Core.Definitions;

internal static class StateLimits
{
    public const float MinNearPlane = 0.1f;
    public const float MaxNearPlane = 4f;

    public const float MinFarPlane = 5;
    public const float MaxFarPlane = 10_000;

    public const float MinFov = 10;
    public const float MaxFov = 180;
}