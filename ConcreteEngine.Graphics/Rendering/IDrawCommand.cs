#region

using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Rendering;

public interface IDrawCommand
{
    public int MeshId { get; }
    public int ShaderId { get; }
    public int TextureId { get; }
    public Matrix4X4<float> Transform { get; }
    void Execute(IGraphicsContext ctx);
}