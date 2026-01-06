using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Loader.Data;

internal enum TextureAnisotropyProfile : byte
{
    Off = 0,
    Default = 1,
    X2 = 2,
    X4 = 3,
    X8 = 4,
    X16 = 5
}

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