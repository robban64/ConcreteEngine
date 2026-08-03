namespace ConcreteEngine.Graphics.Gfx;

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
