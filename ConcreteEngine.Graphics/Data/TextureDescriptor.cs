#region

using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Data;

public readonly record struct TextureDescriptor(
    byte[] PixelData,
    int Width,
    int Height,
    EnginePixelFormat Format,
    TexturePreset Preset,
    float LodBias
);