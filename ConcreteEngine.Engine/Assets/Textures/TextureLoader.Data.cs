using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Resources.Handles;

namespace ConcreteEngine.Engine.Assets.Textures;

internal readonly struct TextureCreationInfo(TextureId textureId, int width, int height, TexturePixelFormat pixelFormat)
{
    public readonly TextureId TextureId = textureId;
    public readonly int Width = width;
    public readonly int Height = height;
    public readonly TexturePixelFormat PixelFormat = pixelFormat;
}

internal readonly struct CubeMapCreationInfo(TextureId textureId, int size, TexturePixelFormat pixelFormat)
{
    public readonly TextureId TextureId = textureId;
    public readonly int Size = size;
    public readonly TexturePixelFormat PixelFormat = pixelFormat;
}

internal readonly struct TextureUploadMeta(GfxTextureDescriptor textureDesc, GfxTextureProperties textureProps)
{
    public readonly GfxTextureDescriptor TextureDesc = textureDesc;
    public readonly GfxTextureProperties TextureProps = textureProps;
}

internal ref struct TextureImportResult
{
    public byte[]? Data;
    public required AssetFileSpec FileSpec;
    public required TextureCreationInfo CreationInfo;
    public required GfxTextureDescriptor TextureDesc;
    public required GfxTextureProperties TextureProps;
}

internal ref struct CubeMapImportResult
{
    public required AssetFileSpec[] FaceFiles;
    public required CubeMapCreationInfo CreationInfo;
    public required GfxTextureDescriptor TextureDesc;
    public required GfxTextureProperties TextureProps;
}