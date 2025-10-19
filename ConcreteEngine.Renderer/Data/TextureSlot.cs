#region

using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Renderer.Data;

public readonly record struct TextureSlotInfo(
    TextureId Texture,
    TextureSlotKind SlotKind,
    TextureKind TextureKind
);

public readonly record struct TextureSlot(TextureId Texture, int Slot)
{
    public static TextureSlot Slot0(TextureId id) => new(id, 0);
    public static TextureSlot Slot1(TextureId id) => new(id, 1);
    public static TextureSlot Slot2(TextureId id) => new(id, 2);
    public static TextureSlot Slot3(TextureId id) => new(id, 3);
    public static TextureSlot Slot4(TextureId id) => new(id, 4);
}