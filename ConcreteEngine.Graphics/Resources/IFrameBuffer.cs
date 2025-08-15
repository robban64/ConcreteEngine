using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Resources;

public interface IFrameBuffer : IGraphicsResource
{
    public ushort ColorTextureId { get;  }
    
    public Vector2D<int> Size { get; set; }

}