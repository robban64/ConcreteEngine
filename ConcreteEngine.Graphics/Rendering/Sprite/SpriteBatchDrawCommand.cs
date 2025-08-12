#region

using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Rendering.Sprite;

public sealed class SpriteBatchDrawCommand : IDrawCommand
{
    public ushort MeshId { get; set; }
    public ushort ShaderId { get; set;}
    public ushort TextureId { get; set;}
    public uint DrawCount { get; set;}

    private Matrix4X4<float> _transform;

    public Matrix4X4<float> Transform
    {
        get => _transform;
        set => _transform = value;
    }


    public void Execute(IGraphicsContext ctx)
    {
        ctx.UseShader(ShaderId);

        ctx.SetUniform(ShaderUniform.ModelMatrix, in _transform);
        ctx.SetUniform(ShaderUniform.SampleTexture, 0);

        ctx.BindTexture(TextureId, 0);

        ctx.BindMesh(MeshId);
        ctx.DrawIndexed(DrawCount);
    }
}