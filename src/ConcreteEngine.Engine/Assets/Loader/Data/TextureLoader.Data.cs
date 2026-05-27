using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Assets.Loader.Data;

internal readonly struct TextureCreationInfo(TextureId textureId, Size3D size)
{
    public readonly Size3D Size = size;
    public readonly TextureId TextureId = textureId;
}

internal readonly struct TextureUploadMeta(Size3D size, CreateTextureProps textureProps)
{
    public readonly Size3D Size = size;
    public readonly CreateTextureProps TextureProps = textureProps;
}