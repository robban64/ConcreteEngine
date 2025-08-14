using ConcreteEngine.Core.Assets;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;

namespace ConcreteEngine.Core.Rendering.Materials;

public readonly record struct MaterialDescription(
    Shader Shader,
    Texture2D Texture,
    BlendMode Blend = BlendMode.Alpha
);

public readonly record struct MaterialId(int Id)
{
    public static MaterialId Of(int id) => new(id);
}

public sealed class Material
{
    private readonly MaterialId _id;
    private readonly Texture2D[] _textures;
    private readonly Shader _shader;
    private readonly BlendMode _blend;

    public MaterialId Id => _id;

    public Texture2D[] Textures => _textures;

    public Shader Shader => _shader;

    public BlendMode Blend => _blend;


    internal Material(
        MaterialId id,
        Texture2D texture,
        Shader shader,
        BlendMode blend = BlendMode.Alpha)
    {
        _id = id;
        _textures = [texture];
        _shader = shader;
        _blend = blend;
    }

    public void Bind(IGraphicsContext ctx)
    {
        ctx.SetBlendMode(_blend);
        ctx.UseShader(_shader.ResourceId);
        //ctx.SetUniform(ShaderUniform.SampleTexture, 0);
        for (int i = 0; i < _textures.Length; i++)
        {
            ctx.BindTexture(_textures[i].ResourceId, (uint)i);

        }
    }
}