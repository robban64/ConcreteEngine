namespace ConcreteEngine.Graphics.Gfx;

public enum GfxUniformType : byte
{
    Unknown,
    Sampler2D,
    IntSampler2D,
    Sampler2DArray,
    Sampler2DShadow,
    Sampler2DMultisample,
    SamplerCube,
    Sampler3D,
    IntSampler3D,
}

public readonly struct GfxUniformSampler(string name, byte binding, GfxUniformType uniformType)
{
    public readonly string Name = name;
    public readonly byte Binding = binding;
    public readonly GfxUniformType UniformType = uniformType;
}