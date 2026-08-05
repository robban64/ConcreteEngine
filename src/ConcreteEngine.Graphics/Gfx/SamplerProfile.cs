using ConcreteEngine.Core.Common.Attributes;

namespace ConcreteEngine.Graphics.Gfx;

[EnumExt]
public enum SamplerSlot : byte
{
    Albedo = 0,
    Normal = 1,
    Mask = 2,
    ShadowMap = 3
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
