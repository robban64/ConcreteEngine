namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class AnimationClip
{
    public readonly string Name;
    public readonly float Duration;
    public readonly float TicksPerSecond;

    public readonly int ActiveChannelCount;

    internal AnimationClip(string name, int activeChannelCount, float duration, float ticksPerSecond)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(activeChannelCount);
        ArgumentOutOfRangeException.ThrowIfNegative(duration);
        ArgumentOutOfRangeException.ThrowIfNegative(ticksPerSecond);

        Name = name;
        ActiveChannelCount = activeChannelCount;
        Duration = duration;
        TicksPerSecond = ticksPerSecond;
    }
}