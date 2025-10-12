using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.Assets.Textures;

internal record struct TextureCreationInfo(
    TextureId TextureId,
    int Width,
    int Height,
    TexturePixelFormat PixelFormat);

internal record struct CubeMapCreationInfo(TextureId TextureId, int Size, TexturePixelFormat PixelFormat);

internal sealed record TexturePayload(
    byte[] Data,
    GfxTextureDescriptor TextureDesc,
    GfxTextureProperties TextureProps,
    in AssetFileSpec FileSpec
);

internal sealed record CubeMapPayload(
    ReadOnlyMemory<byte>[] FaceData,
    AssetFileSpec[] FaceFiles,
    GfxTextureDescriptor TextureDesc,
    GfxTextureProperties TextureProps
);