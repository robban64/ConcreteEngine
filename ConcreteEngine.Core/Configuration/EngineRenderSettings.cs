namespace ConcreteEngine.Core.Configuration;

public record EngineRenderSettings
{
    public bool VSync { get; init; } = true;
    public double Fps { get; init; } = 0.0;
}