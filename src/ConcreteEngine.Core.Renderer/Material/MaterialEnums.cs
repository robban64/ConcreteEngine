namespace ConcreteEngine.Core.Renderer.Material;

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

public enum TextureUsage : byte
{
    Albedo = 0,
    Normal = 1,
    Mask = 2,
    Specular = 3,
    Emissive = 4,
    Environment = 5,
    Shadowmap = 6,
    Lightmap = 7,
    Splatmap = 8,
    Heightmap = 9,
    Custom0 = 10,
    Custom1 = 11
}