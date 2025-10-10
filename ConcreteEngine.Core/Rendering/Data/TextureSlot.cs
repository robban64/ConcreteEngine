using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Data;

public readonly record struct TextureSlot(TextureId Id, int Slot)
{
    public static TextureSlot Slot0(TextureId id) => new(id, 0);
    public static TextureSlot Slot1(TextureId id) => new(id, 1);
    public static TextureSlot Slot2(TextureId id) => new(id, 2);
    public static TextureSlot Slot3(TextureId id) => new(id, 3);
    public static TextureSlot Slot4(TextureId id) => new(id, 4);
}