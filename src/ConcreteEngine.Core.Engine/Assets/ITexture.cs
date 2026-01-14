using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Assets;

public interface ITexture : IAsset
{
    TextureId GfxId { get; }

    int Width { get; }
    int Height { get; }

    TexturePreset Preset { get; }
    TextureKind TextureKind { get; }
    TexturePixelFormat PixelFormat { get; }
    TextureAnisotropyProfile Anisotropy { get; }

    TextureUsage Usage { get; }
}