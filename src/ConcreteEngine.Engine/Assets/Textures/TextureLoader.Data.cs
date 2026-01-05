using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Textures;

internal readonly struct TextureCreationInfo(TextureId textureId, int width, int height)
{
    public readonly TextureId TextureId = textureId;
    public readonly int Width = width;
    public readonly int Height = height;
}

internal readonly struct TextureUploadMeta(CreateTextureInfo textureDesc, CreateTextureProps textureProps)
{
    public readonly CreateTextureInfo TextureDesc = textureDesc;
    public readonly CreateTextureProps TextureProps = textureProps;
}