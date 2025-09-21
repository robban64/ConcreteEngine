namespace ConcreteEngine.Graphics;

public enum EnginePixelFormat : byte
{
    Red = 0,
    Rgb = 1,
    Rgba = 2,
    Srgb8Alpha8 = 3
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

public enum TextureAnisotropy : byte
{
    Off = 0,
    Default = 1,
    X2 = 2,
    X4 = 4,
    X8 = 8,
    X16 = 16
}