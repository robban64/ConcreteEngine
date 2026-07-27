using ConcreteEngine.Core.Common.Attributes;

namespace ConcreteEngine.Core.Engine.Graphics;

public enum SurfaceNormalMode : byte
{
    None = 0,
    VertexOnly = 1,
    TangentSpaceNormalMap = 2
}

public enum AlphaMode : byte
{
    Opaque = 0,
    Cutout = 1,
    Transparent = 2
}

public enum ShadingModelMode : byte
{
    Unlit = 0,
    Lambert = 1,
    BlinnPhong = 2
}
