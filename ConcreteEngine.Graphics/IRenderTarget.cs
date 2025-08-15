#region

using System.Drawing;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics;

public interface IRenderTarget : IGraphicsResource
{
    public ITexture2D? Texture { get; }

    public Rectangle<int> ViewportSize { get; set; }

    void Clear(Color color);
}