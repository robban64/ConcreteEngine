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
    Off,
    Default,
    X2,
    X4,
    X8,
    X16
}