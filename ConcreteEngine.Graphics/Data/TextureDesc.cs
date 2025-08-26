#region

using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Data;

public readonly record struct TextureDesc(
    byte[] PixelData,
    int Width,
    int Height,
    EnginePixelFormat Format,
    TexturePreset Preset,
    float LodBias = 0,
    bool NullPtrData = false
);