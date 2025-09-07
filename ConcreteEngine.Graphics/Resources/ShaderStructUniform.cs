using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics;

using static ShaderStructUniform;

public enum ShaderStructUniform : byte
{
    DirLight,
    Material
}


public static class ShaderStructUniforms
{
    public static ShaderStructUniform ToUniform(ReadOnlySpan<char> uniform)
    {
        return uniform switch
        {
            "uLight" => DirLight,
            "uMaterial" => Material,
            _ => throw new GraphicsException($"Unknown struct uniform: {uniform}")
        };
    }
}