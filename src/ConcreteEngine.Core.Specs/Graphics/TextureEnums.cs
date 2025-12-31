namespace ConcreteEngine.Core.Specs.Graphics;

public enum TexturePixelFormat : byte
{
    Unknown = 0,
    Rgb = 1,
    Rgba = 2,
    SrgbAlpha = 3,
    Depth = 4,
    Red = 5
}

public enum TextureKind : byte
{
    Unknown = 0,
    Texture2D = 1,
    Texture3D = 2,
    CubeMap = 3,
    Multisample2D = 4
}

public enum TexturePreset : byte
{
    None = 0,
    NearestClamp = 1,
    NearestClampBorder = 2,
    NearestRepeat = 3,
    LinearClamp = 4,
    LinearClampBorder = 5,
    LinearRepeat = 6,
    LinearMipmapClamp = 7,
    LinearMipmapRepeat = 8,
    PremultipliedUi = 9
}

public enum TextureAnisotropy : byte
{
    Off = 0,
    X2 = 2,
    X4 = 4,
    X8 = 8,
    X16 = 16
}