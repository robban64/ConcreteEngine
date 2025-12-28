using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Renderer.Data;

public readonly struct TextureSlotInfo(
    TextureId texture,
    TextureSlotKind slotKind,
    TextureKind textureKind
)
{
    public readonly TextureId Texture = texture;
    public readonly TextureSlotKind SlotKind = slotKind;
    public readonly TextureKind TextureKind = textureKind;
}

public readonly struct TextureSlot(TextureId texture, int slot)
{
    public readonly TextureId Texture = texture;
    public readonly int Slot = slot;

    public static TextureSlot Slot0(TextureId id) => new(id, 0);
    public static TextureSlot Slot1(TextureId id) => new(id, 1);
    public static TextureSlot Slot2(TextureId id) => new(id, 2);
    public static TextureSlot Slot3(TextureId id) => new(id, 3);
    public static TextureSlot Slot4(TextureId id) => new(id, 4);
}