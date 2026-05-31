namespace ConcreteEngine.Renderer.Core;

public readonly struct TextureBinding(TextureId texture, TextureUsage slotKind, sbyte slot)
{
    public readonly TextureId Texture = texture;
    public readonly TextureUsage SlotKind = slotKind;
    public readonly sbyte Slot = slot;
}