using ConcreteEngine.Graphics.Gfx.Definitions;

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

    public TextureAnisotropy GetClampedAnisotropy(TextureAnisotropy anisotropy)
    {
        
        TextureAnisotropy max;
        if (anisotropy == TextureAnisotropy.Off) return TextureAnisotropy.Off;
        if (anisotropy == TextureAnisotropy.Default)
        {
            if (TextureQuality == EngineGraphicsLevel.Low) return TextureAnisotropy.X2;
            return TextureAnisotropy.X4;
        }

        switch (TextureQuality)
        {
            case EngineGraphicsLevel.Low:
                return TextureAnisotropy.X2;
            case EngineGraphicsLevel.Medium:
                return  (int)anisotropy <= (int)TextureAnisotropy.X4 ? anisotropy : TextureAnisotropy.X4;
            case EngineGraphicsLevel.High:
                return anisotropy;
            default:
                throw new ArgumentOutOfRangeException(nameof(TextureQuality));
        }


        return (TextureAnisotropy)int.Min((int)anisotropy, (int)max);
    }
}