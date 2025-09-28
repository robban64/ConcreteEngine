#region

using static ConcreteEngine.Graphics.Resources.ShaderUniform;

#endregion

namespace ConcreteEngine.Graphics.Resources;

//TODO remove most of these, as most are no longer used
public enum ShaderUniform : byte
{
    ModelMatrix = 0,
    ViewMatrix = 1,
    ProjectionMatrix = 2,
    ProjectionViewMatrix = 3,
    NormalMatrix = 4,
    TextureOffset = 5,
    TextureScale = 6,
    TexCoordRepeat = 7,
    TexelSize = 8,
    Time = 9,
}

public static class ShaderUniforms
{
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
            Time => "uTime",
            TexelSize => "uTexelSize",
            _ => throw new ArgumentOutOfRangeException(nameof(uniform), uniform, null)
        };
    }
}