#region

using static ConcreteEngine.Graphics.Resources.ShaderUniform;

#endregion

namespace ConcreteEngine.Graphics.Resources;

//TODO remove most of these, as most are no longer used
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

    SamplerTexture,
    SamplerNormal,
    SamplerScene,
    SamplerLight,
    SamplerBloom,
    
    SampleCubemap,

    Color,
    Ambient,
    
    TexelSize,
    LightPos,
    CameraPos,

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
        [SamplerTexture, SamplerNormal, SamplerScene, SamplerLight, SamplerBloom, SampleCubemap];

    public static readonly ShaderUniform[] Matrix4Uniforms =
        [ModelMatrix, NormalMatrix, ViewMatrix, ProjectionMatrix, ProjectionViewMatrix];

    public static readonly ShaderUniform[] VectorUniforms =
        [Color, Ambient, TexelSize, LightPos, CameraPos];
    

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
            SamplerTexture => "uTexture",
            SamplerNormal => "uNormalTex",
            SamplerScene => "uSceneTex",
            SamplerLight => "uLightTex",
            SamplerBloom => "uBloomTex",
            SampleCubemap => "uCubemapTex",
            Time => "uTime",
            TexelSize => "uTexelSize",
            Threshold => "uThreshold",
            SoftKnee => "uSoftKnee",
            LightPos => "uLightPos",
            CameraPos => "uCameraPos",
            Radius => "uRadius",
            Color => "uColor",
            Ambient => "uAmbient",
            Intensity => "uIntensity",
            Softness => "uSoftness",
            Shape => "uShape",
            _ => throw new ArgumentOutOfRangeException(nameof(uniform), uniform, null)
        };
    }
}