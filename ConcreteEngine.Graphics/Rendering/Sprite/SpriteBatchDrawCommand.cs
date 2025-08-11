#region

using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Rendering.Sprite;

public sealed class SpriteBatchDrawCommand : IDrawCommand
{
    public IMesh Mesh { get; set; }
    public IShader Shader { get; set; }
    public ITexture2D Texture { get; set; }

    private Matrix4X4<float> _transform;

    public Matrix4X4<float> Transform
    {
        get => _transform;
        set => _transform = value;
    }


    public void Execute(IGraphicsContext ctx)
    {
        ctx.UseShader(Shader);

        ctx.SetUniform(ShaderUniform.ModelMatrix, in _transform);
        ctx.SetUniform(ShaderUniform.SampleTexture, 0);

        ctx.BindTexture(0, Texture);

        ctx.BindMesh(Mesh);
        ctx.DrawIndexed(Mesh.DrawCount);
    }
}