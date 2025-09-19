using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Contracts;

public readonly record struct GpuTextureDescriptor(
    int Width,
    int Height,
    TexturePreset Preset,
    TextureKind Kind,
    EnginePixelFormat Format = EnginePixelFormat.Rgba,
    TextureAnisotropy Anisotropy = TextureAnisotropy.Default,
    float LodBias = 0
);