#region

using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.Contracts;

public readonly record struct GpuTextureDescriptor(
    int Width,
    int Height,
    TexturePreset Preset,
    TextureKind Kind,
    EnginePixelFormat Format = EnginePixelFormat.Rgba,
    TextureAnisotropy Anisotropy = TextureAnisotropy.Default,
    float LodBias = 0
)
{
    public static GpuTextureDescriptor MakeFboMsaaDesc(int width, int height) =>
        new(width, height, TexturePreset.None, TextureKind.Multisample2D, EnginePixelFormat.Srgb8Alpha8,
            TextureAnisotropy.Off, 0);

    public static GpuTextureDescriptor MakeFboColorDesc(int width, int height) =>
        new(width, height, TexturePreset.None, TextureKind.Texture2D, EnginePixelFormat.Srgb8Alpha8,
            TextureAnisotropy.Off, 0);
}