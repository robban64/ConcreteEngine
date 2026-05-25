using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class TextureArray
{
    private readonly Texture?[] _textures;
    public TextureId GfxId { get; private set; }

    public TextureArray(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 2);
        _textures = new Texture[length];
    }

    public void SetTexture(int index, Texture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_textures.Length, nameof(index));
        _textures[index] = texture;
    }

    internal TextureId Compile(GfxTextures gfx)
    {
        for (int i = 0; i < _textures.Length; i++)
        {
            if (_textures[i] == null) throw new InvalidOperationException($"Texture {i} is null");
            if (_textures[i]!.GfxId == default) throw new InvalidOperationException($"Texture {i} has empty TextureId");
        }

        var arrayId = gfx.CreateTexture2DArrayFrom(_textures[0]!.GfxId, _textures.Length);
        for (int i = 0; i < _textures.Length; i++)
            gfx.SetTexture2DArrayLayerFrom(arrayId, _textures[i]!.GfxId, i);

        return GfxId = arrayId;
    }

}