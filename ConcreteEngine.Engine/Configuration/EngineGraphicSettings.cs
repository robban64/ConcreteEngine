namespace ConcreteEngine.Engine.Configuration;

public enum EngineGraphicsLevel : byte
{
    Low,
    Medium,
    High
}

public sealed class EngineGraphicSettings
{
    public int UpdateFps { get; init; } = 60;
    public int RenderFps { get; init; } = 144;
    public bool Vsync { get; init; }
    public EngineGraphicsLevel ShadowQuality { get; init; } = EngineGraphicsLevel.High;
    public EngineGraphicsLevel TextureQuality { get; init; } = EngineGraphicsLevel.High;

    public void Validate()
    {
        if (UpdateFps > RenderFps || UpdateFps < 20 || RenderFps < 20 || UpdateFps > 200 || RenderFps > 300)
            throw new InvalidOperationException();
    }
}