using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Assets;

public interface ITexture : IAsset
{
    TextureId GfxId { get; }

    Size2D Size { get; }
    int MipLevels { get; }
    float LodBias { get; }

    TexturePreset Preset { get; }
    TextureKind TextureKind { get; }
    TexturePixelFormat PixelFormat { get; }
    TextureAnisotropyProfile Anisotropy { get; }

    TextureUsage Usage { get; }
}