namespace ConcreteEngine.Graphics;

public enum BlendMode : byte
{
    Unset = 0,
    None = 1,
    Alpha = 1,
    PremultipliedAlpha = 2,
    Additive = 3,
}

public enum ClearBufferFlag : byte
{
    None = 0,
    Color = 1,
    Depth = 2,
    ColorAndDepth = 3
}

public enum CullMode : byte
{
    Unset = 0,
    None = 1,
    BackCcw = 2,
    BackCw = 3,
    FrontCcw = 4,
    FrontCw = 5
}

public enum DepthMode : byte
{
    Unset = 0,
    Disabled = 1,
    ReadOnlyLequal = 2,
    WriteLequal = 3,
    WriteLess = 4
}