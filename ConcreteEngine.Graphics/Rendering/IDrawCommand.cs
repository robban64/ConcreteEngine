#region

using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Rendering;

public interface IDrawCommand
{
    public IMesh Mesh { get; }
    public IShader Shader { get; }
    public ITexture2D Texture { get; }
    public Matrix4X4<float> Transform { get; }
    void Execute(IGraphicsContext ctx);
}