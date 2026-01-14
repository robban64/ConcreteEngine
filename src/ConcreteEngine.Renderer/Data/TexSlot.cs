using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Renderer.Data;

public readonly struct TexSlot(TextureId texture, int slot)
{
    public readonly TextureId Texture = texture;
    public readonly int Slot = slot;

    public static TexSlot Slot0(TextureId id) => new(id, 0);
    public static TexSlot Slot1(TextureId id) => new(id, 1);
    public static TexSlot Slot2(TextureId id) => new(id, 2);
    public static TexSlot Slot3(TextureId id) => new(id, 3);
    public static TexSlot Slot4(TextureId id) => new(id, 4);
}