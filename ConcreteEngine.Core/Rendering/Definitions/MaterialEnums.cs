namespace ConcreteEngine.Core.Rendering.Definitions;

public enum SurfaceNormalMode : byte
{
    None,
    VertexOnly,
    TangentSpaceNormalMap
}

public enum AlphaMode : byte
{
    Opaque,
    Cutout,
    Transparent
}

public enum ShadingModelMode : byte
{
    Unlit,
    Lambert,
    BlinnPhong
}

public enum TextureSlotKind : byte
{
    Albedo,
    Normal,
    Specular,
    Emissive,
    Environment,
    Lightmap,
    Custom0,
    Custom1
}