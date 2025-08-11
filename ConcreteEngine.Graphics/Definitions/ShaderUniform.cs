namespace ConcreteEngine.Graphics.Definitions;

public enum ShaderUniform : byte
{
    ModelMatrix,
    ProjectionViewMatrix,
    TextureOffset,
    TextureScale,
    SampleTexture
}

public static class ShaderUniforms
{
    // Vertex Shader
    public const string ModelName = "uModel";
    public const string ProjectionViewName = "uViewProj";
    public const string TextureOffsetName = "uTexOffset";
    public const string TextureScaleName = "uTexScale";

    // Fragment Shader
    public const string SamplerTextureName = "uTexture";

    public static string ToUniformName(this ShaderUniform uniform)
    {
        return uniform switch
        {
            ShaderUniform.ModelMatrix => ModelName,
            ShaderUniform.ProjectionViewMatrix => ProjectionViewName,
            ShaderUniform.TextureOffset => TextureOffsetName,
            ShaderUniform.TextureScale => TextureScaleName,
            ShaderUniform.SampleTexture => SamplerTextureName,
        };
    }
}