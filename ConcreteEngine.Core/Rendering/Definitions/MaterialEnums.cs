namespace ConcreteEngine.Core.Rendering.Definitions;

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

public enum TextureSlotKind : byte
{
    Albedo = 0,
    Normal = 1,
    Specular = 2,
    Emissive = 3,
    Environment = 4,
    Shadowmap = 5,
    Lightmap = 6,
    Splatmap = 7,
    Heightmap = 8,
    Custom0 = 9,
    Custom1 = 10
}