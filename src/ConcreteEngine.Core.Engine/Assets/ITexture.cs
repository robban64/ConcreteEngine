using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Assets;

public interface ITexture : IAsset
{
    TextureId GfxId { get; }

    int Width { get; }
    int Height { get; }

    MaterialSlotKind SlotKind { get; }
}