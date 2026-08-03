using ConcreteEngine.Core.Common.Attributes;

namespace ConcreteEngine.Graphics.Gfx;

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
