namespace ConcreteEngine.Core.Engine.Graphics;

public readonly struct TextureBinding(TextureId texture, TextureUsage slotKind, byte slot)
{
    public readonly TextureId Texture = texture;
    public readonly TextureUsage SlotKind = slotKind;
    public readonly byte Slot = slot;
}