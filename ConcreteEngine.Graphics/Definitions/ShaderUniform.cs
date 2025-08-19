namespace ConcreteEngine.Graphics.Definitions;

public enum ShaderUniform : byte
{
    ModelMatrix,
    ProjectionMatrix,
    ProjectionViewMatrix,
    TextureOffset,
    TextureScale,

    SampleTexture,
    SamplerScene,
    SamplerLight,
    SamplerBloom,

    Time,
    TexelSize,
    Threshold,
    SoftKnee,
    
    LightPos,
    Radius,
    Color,
    Intensity,
    Softness,
    Shape,

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
            ShaderUniform.ProjectionMatrix => "uProj",
            ShaderUniform.TextureOffset => "uTexOffset",
            ShaderUniform.TextureScale => "uTexScale",
            ShaderUniform.SampleTexture => "uTexture",
            ShaderUniform.SamplerScene => "uSceneTex",
            ShaderUniform.SamplerLight => "uLightTex",
            ShaderUniform.SamplerBloom => "uBloomTex",
            ShaderUniform.Time => "uTime",
            ShaderUniform.TexelSize => "uTexelSize",
            ShaderUniform.Threshold => "uThreshold",
            ShaderUniform.SoftKnee => "uSoftKnee",
            ShaderUniform.LightPos => "uLightPos",
            ShaderUniform.Radius => "uRadius",
            ShaderUniform.Color => "uColor",
            ShaderUniform.Intensity => "uIntensity",
            ShaderUniform.Softness => "uSoftness",
            ShaderUniform.Shape => "uShape",
            ShaderUniform.BloomStrength => "uBloomStrength",
            ShaderUniform.VignetteRadius => "uVignetteRadius",
            ShaderUniform.VignetteSoft => "uVignetteSoft",
            ShaderUniform.VignetteGain => "uVignetteGain",
            _ => throw new ArgumentOutOfRangeException(nameof(uniform), uniform, null)
        };
    }
}