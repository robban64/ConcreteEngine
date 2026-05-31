using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Assets.Loader.Data;


internal readonly struct TextureUploadMeta(Size3D size, CreateTextureProps textureProps)
{
    public readonly Size3D Size = size;
    public readonly CreateTextureProps TextureProps = textureProps;
}