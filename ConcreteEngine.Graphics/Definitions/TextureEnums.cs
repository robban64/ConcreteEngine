namespace ConcreteEngine.Graphics;

public enum EnginePixelFormat : byte
{
    Red = 0,
    Rgb = 1,
    Rgba = 2
}

public enum TexturePreset : byte
{
    NearestClamp = 0,
    NearestRepeat = 1, 
    LinearClamp = 2, 
    LinearRepeat = 3, 
    LinearMipmapClamp = 4,
    LinearMipmapRepeat = 5,
    PremultipliedUi = 6
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