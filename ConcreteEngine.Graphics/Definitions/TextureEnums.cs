namespace ConcreteEngine.Graphics;

public enum EnginePixelFormat : byte
{
    Rgb = 0,
    Rgba = 1,
    SrgbAlpha = 2
}

public enum TexturePreset : byte
{
    None = 0,
    NearestClamp = 1,
    NearestRepeat = 2,
    LinearClamp = 3,
    LinearRepeat = 4,
    LinearMipmapClamp = 5,
    LinearMipmapRepeat = 6,
    PremultipliedUi = 7
}

// note not using the enum values, due to risk of changing stuff
public enum TextureAnisotropy : byte
{
    Off = 0,
    Default = 1,
    X2 = 2,
    X4 = 4,
    X8 = 8,
    X16 = 16
}