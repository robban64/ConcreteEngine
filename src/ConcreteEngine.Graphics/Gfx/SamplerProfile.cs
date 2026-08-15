using ConcreteEngine.Core.Common.Attributes;

namespace ConcreteEngine.Graphics.Gfx;

[EnumExt]
public enum SamplerSlot : byte
{
    Diffuse = 0,
    Normal = 1,
    Specular = 2,
    Emissive = 3,
    AlphaMask = 4,

    DetailMap = 5,
    EnvironmentMap = 6,
    FeatureMap0 = 7,
    FeatureMap1 = 8,

    ShadowMap0 = 9,
    ShadowMap1 = 10,

    LightMap = 11,
    AmbientOcclusion = 12
}

[EnumExt]
public enum SamplerProfile : byte
{
    PointClamp,
    PointWrap,

    LinearClamp,
    LinearWrap,

    TrilinearClamp,
    TrilinearWrap,

    AnisotropicWrap,
    AnisotropicClamp,

    ShadowCompare,
}