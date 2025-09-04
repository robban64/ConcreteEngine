#region

#endregion

namespace ConcreteEngine.Graphics.Descriptors;

public readonly record struct TextureDesc(
    byte[] PixelData,
    int Width,
    int Height,
    EnginePixelFormat Format,
    TexturePreset Preset,
    int? Anisotropy = null,
    float LodBias = 0,
    bool NullPtrData = false
);