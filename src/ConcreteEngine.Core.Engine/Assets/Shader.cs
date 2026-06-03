using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Assets;

public struct ShaderDefaultBinding
{
    public sbyte AlbedoBinding;
    public sbyte NormalBinding;
    public sbyte SpecularBinding;
    public sbyte AlphaBinding;
    public sbyte ShadowMapBinding;
    public sbyte LightMapBinding;
    public sbyte CubeMapBinding;

    public static ShaderDefaultBinding MakeReset() =>
        new()
        {
            AlbedoBinding = -1,
            NormalBinding = -1,
            SpecularBinding = -1,
            AlphaBinding = -1,
            ShadowMapBinding = -1,
            LightMapBinding = -1,
            CubeMapBinding = -1
        };
}

public sealed class Shader : AssetObject
{
    public const string AlbedoName = "uTexture";
    public const string NormalName = "uNormal";
    public const string SpecularName = "uSpecular";
    public const string AlphaName = "uAlpha";
    public const string LightMapName = "uLightMap";

    public readonly ShaderId GfxId;
    public GfxUniformSampler[] Samplers { get; private set; } = [];

    public ShaderDefaultBinding DefaultBindings;

    public Shader(string name, AssetId id, Guid gid, ShaderId gfxId, GfxUniformSampler[] samplers) : base(name,id,gid)
    {
        GfxId = gfxId;
        SetSamplers(samplers);
    }

    internal void SetSamplers(GfxUniformSampler[] samplers)
    {
        ArgumentNullException.ThrowIfNull(samplers);
        Samplers = samplers;

        var bindings = ShaderDefaultBinding.MakeReset();
        foreach (var sampler in samplers)
        {
            var samplerBinding = (sbyte)sampler.Binding;

            switch (sampler.Name)
            {
                case AlbedoName:
                    bindings.AlbedoBinding = samplerBinding;
                    continue;
                case NormalName:
                    bindings.NormalBinding = samplerBinding;
                    continue;
                case SpecularName:
                    bindings.SpecularBinding = samplerBinding;
                    continue;
                case AlphaName:
                    bindings.AlphaBinding = samplerBinding;
                    continue;
                case LightMapName:
                    bindings.LightMapBinding = samplerBinding;
                    continue;
            }

            if (sampler.UniformType == GfxUniformType.SamplerCube)
                bindings.CubeMapBinding = samplerBinding;
            else if (sampler.UniformType == GfxUniformType.Sampler2DShadow)
                bindings.ShadowMapBinding = samplerBinding;
        }

        DefaultBindings = bindings;
    }

    public override AssetKind Kind => AssetKind.Shader;
    public override AssetCategory Category => AssetCategory.Graphic;
}