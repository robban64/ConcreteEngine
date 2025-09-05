#region

using static ConcreteEngine.Graphics.ShaderUniform;

#endregion

namespace ConcreteEngine.Graphics;

public enum ShaderUniform : byte
{
    ModelMatrix,
    ViewMatrix,
    ProjectionMatrix,
    ProjectionViewMatrix,
    NormalMatrix,
    
    TextureOffset,
    TextureScale,
    TexCoordRepeat,

    SampleTexture,
    SamplerScene,
    SamplerLight,
    SamplerBloom,
    
    SampleCubemap,

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
        [SampleTexture, SamplerScene, SamplerLight, SamplerBloom, SampleCubemap];

    public static readonly ShaderUniform[] Matrix4Uniforms =
        [ModelMatrix, NormalMatrix, ViewMatrix, ProjectionMatrix, ProjectionViewMatrix];

    public static readonly ShaderUniform[] VectorUniforms =
        [Color, TexelSize, LightPos];

    public static readonly ShaderUniform[] PrimitiveUniforms =
        [Time, Threshold, SoftKnee, Radius, Intensity, Softness, Shape];

    public static string ToUniformName(this ShaderUniform uniform)
    {
        return uniform switch
        {
            ModelMatrix => "uModel",
            ProjectionMatrix => "uProj",
            ViewMatrix => "uView",
            ProjectionViewMatrix => "uViewProj",
            NormalMatrix => "uNormalMat",
            TextureOffset => "uTexOffset",
            TextureScale => "uTexScale",
            TexCoordRepeat => "uTexCoordRepeat",
            SampleTexture => "uTexture",
            SamplerScene => "uSceneTex",
            SamplerLight => "uLightTex",
            SamplerBloom => "uBloomTex",
            SampleCubemap => "uCubemapTex",
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