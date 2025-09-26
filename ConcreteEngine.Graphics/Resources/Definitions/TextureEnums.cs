namespace ConcreteEngine.Graphics.Resources;

public enum TextureKind : byte
{
    Unknown = 0,
    Texture2D = 1,
    Texture3D = 2,
    CubeMap = 3,
    Multisample2D = 4
}