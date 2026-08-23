namespace ConcreteEngine.Graphics.Gfx;

public readonly struct TextureBinding(TextureId texture, SamplerSlot slot, SamplerProfile profile)
{
    public readonly TextureId Texture = texture;
    public readonly SamplerSlot Slot = slot;
    public readonly SamplerProfile Profile = profile;
}