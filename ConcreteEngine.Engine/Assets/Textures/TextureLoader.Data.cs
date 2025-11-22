#region

using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

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

internal sealed class TextureImportResult
{
    public  byte[]? Data { get; init; } = null;
    public required AssetFileSpec FileSpec  { get; init; }
    public required TextureCreationInfo CreationInfo  { get; init; }
    public required GfxTextureDescriptor TextureDesc  { get; init; }
    public required GfxTextureProperties TextureProps  { get; init; }
}

internal sealed class CubeMapImportResult
{
    public required AssetFileSpec[] FaceFiles { get; init; }
    public required CubeMapCreationInfo CreationInfo { get; init; }
    public required GfxTextureDescriptor TextureDesc { get; init; }
    public required GfxTextureProperties TextureProps { get; init; }
}