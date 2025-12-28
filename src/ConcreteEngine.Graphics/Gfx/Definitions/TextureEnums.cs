namespace ConcreteEngine.Graphics.Gfx.Definitions;


public enum TextureFilter : byte
{
    Nearest = 0,
    Linear = 1
}

public enum TextureWrap : byte
{
    Repeat = 0,
    ClampToEdge = 1,
    ClampToBorder = 2
}

public enum TextureCompare : byte
{
    None = 0,
    LessOrEqual = 1,
    GreaterOrEqual = 2
}

