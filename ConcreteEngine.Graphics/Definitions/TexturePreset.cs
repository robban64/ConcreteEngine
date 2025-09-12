namespace ConcreteEngine.Graphics;

public enum TexturePreset : byte
{
    NearestClamp,
    NearestRepeat, 
    LinearClamp, 
    LinearRepeat, 
    LinearMipmapClamp,
    LinearMipmapRepeat,
    PremultipliedUI
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