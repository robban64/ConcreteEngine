using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.Assets.Materials;

public readonly record struct TextureSlotInfo(
    TextureId Texture,
    TextureSlotKind SlotKind,
    TextureKind TextureKind,
    bool Srgb 
);