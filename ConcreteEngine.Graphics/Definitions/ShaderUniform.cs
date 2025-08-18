namespace ConcreteEngine.Graphics.Definitions;

public enum ShaderUniform : byte
{
    ModelMatrix,
    ProjectionViewMatrix,
    TextureOffset,
    TextureScale,

    SampleTexture,
    SamplerScene,
    SamplerBloom,

    TexelSize,
    Threshold,
    SoftKnee,

    BloomStrength,
    VignetteRadius,
    VignetteSoft,
    VignetteGain
}

public static class ShaderUniforms
{
    public static string ToUniformName(this ShaderUniform uniform)
    {
        return uniform switch
        {
            ShaderUniform.ModelMatrix => "uModel",
            ShaderUniform.ProjectionViewMatrix => "uViewProj",
            ShaderUniform.TextureOffset => "uTexOffset",
            ShaderUniform.TextureScale => "uTexScale",
            ShaderUniform.SampleTexture => "uTexture",
            ShaderUniform.SamplerScene => "uSceneTex",
            ShaderUniform.SamplerBloom => "uBloomTex",
            ShaderUniform.TexelSize => "uTexelSize",
            ShaderUniform.Threshold => "uThreshold",
            ShaderUniform.SoftKnee => "uSoftKnee",
            ShaderUniform.BloomStrength => "uBloomStrength",
            ShaderUniform.VignetteRadius => "uVignetteRadius",
            ShaderUniform.VignetteSoft => "uVignetteSoft",
            ShaderUniform.VignetteGain => "uVignetteGain",
            _ => throw new ArgumentOutOfRangeException(nameof(uniform), uniform, null)
        };
    }
}