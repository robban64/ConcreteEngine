namespace ConcreteEngine.Graphics.Gfx;

public readonly struct TextureBinding(TextureId texture, TextureUsage slotKind, byte slot, SamplerProfile profile)
{
    public readonly TextureId Texture = texture;
    public readonly TextureUsage SlotKind = slotKind;
    public readonly byte Slot = slot;
    public readonly SamplerProfile Profile = profile;
}