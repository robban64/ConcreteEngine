#region

using static ConcreteEngine.Graphics.ShaderUniform;

#endregion

namespace ConcreteEngine.Graphics;

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

    Color,
    TexelSize,
    LightPos,

    Time,
    Threshold,
    SoftKnee,
    Radius,
    Intensity,
    Softness,
    Shape
}

public static class ShaderUniforms
{
    public static readonly ShaderUniform[] SamplerUniforms =
        [SampleTexture, SamplerScene, SamplerLight, SamplerBloom];

    public static readonly ShaderUniform[] Matrix4Uniforms =
        [ModelMatrix, ProjectionMatrix, ProjectionViewMatrix];

    public static readonly ShaderUniform[] VectorUniforms =
        [Color, TexelSize, LightPos];

    public static readonly ShaderUniform[] PrimitiveUniforms =
        [Time, Threshold, SoftKnee, Radius, Intensity, Softness, Shape];

    public static string ToUniformName(this ShaderUniform uniform)
    {
        return uniform switch
        {
            ModelMatrix => "uModel",
            ProjectionViewMatrix => "uViewProj",
            ProjectionMatrix => "uProj",
            TextureOffset => "uTexOffset",
            TextureScale => "uTexScale",
            SampleTexture => "uTexture",
            SamplerScene => "uSceneTex",
            SamplerLight => "uLightTex",
            SamplerBloom => "uBloomTex",
            Time => "uTime",
            TexelSize => "uTexelSize",
            Threshold => "uThreshold",
            SoftKnee => "uSoftKnee",
            LightPos => "uLightPos",
            Radius => "uRadius",
            Color => "uColor",
            Intensity => "uIntensity",
            Softness => "uSoftness",
            Shape => "uShape",
            _ => throw new ArgumentOutOfRangeException(nameof(uniform), uniform, null)
        };
    }
}