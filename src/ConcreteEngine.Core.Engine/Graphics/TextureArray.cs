using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Graphics;

public abstract class CompositeTexture
{
    protected readonly Texture?[] Textures;

    public bool IsDirty { get; protected set; }

    public CompositeTexture(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 255);

        Textures = new Texture[length];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasEmptyTextureSlot() => Textures.Contains(null);

    public void SetTexture(int index, Texture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)Textures.Length, nameof(index));
        Textures[index] = texture;
        IsDirty = true;
    }

    internal abstract TextureId Compile(GfxTextures gfx);
}

public sealed class TextureArray(int length) : CompositeTexture(length)
{
    public TextureId GfxId { get; private set; }

    internal override TextureId Compile(GfxTextures gfx)
    {
        if (!IsDirty) return GfxId;
        IsDirty = false;

        for (int i = 0; i < Textures.Length; i++)
        {
            if (Textures[i] == null) throw new InvalidOperationException($"Texture {i} is null");
            if (Textures[i]!.GfxId == default) throw new InvalidOperationException($"Texture {i} has empty TextureId");
        }

        var arrayId = gfx.CreateTexture2DArrayFrom(Textures[0]!.GfxId, Textures.Length);
        for (int i = 0; i < Textures.Length; i++)
            gfx.SetTexture2DArrayLayerFrom(arrayId, Textures[i]!.GfxId, i);

        return GfxId = arrayId;
    }
}