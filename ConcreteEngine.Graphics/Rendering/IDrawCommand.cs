#region

using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Rendering;

public interface IDrawCommand
{
    public ushort MeshId { get; }
    public ushort ShaderId { get; }
    public ushort TextureId { get; }
    public Matrix4X4<float> Transform { get; }
    void Execute(IGraphicsContext ctx);
}