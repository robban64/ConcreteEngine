#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.IO;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using StbImageSharp;

#endregion

namespace ConcreteEngine.Core.Assets.Textures;

internal sealed class TextureLoader
{
    public TexturePayload LoadTexture(TextureDescriptor record)
    {
        //StbImage.stbi_set_flip_vertically_on_load(1);

        var path = AssetPaths.GetTexturePath(record.Filename);

        var fi = new FileInfo(path);
        if (!fi.Exists) throw new FileNotFoundException("File not found.", path);

        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        ValidateImageResult(image);

        var desc = new GfxTextureDescriptor(
            width: image.Width,
            height: image.Height,
            kind: TextureKind.Texture2D,
            format: record.PixelFormat
        );

        var props = new GfxTextureProperties(
            preset: record.Preset,
            anisotropy: record.Anisotropy,
            lodBias: record.LodBias
        );

        var fileSpec = new AssetFileSpec(
            storage: AssetStorageKind.FileSystem,
            logicalName: record.Name,
            relativePath: record.Filename,
            sizeBytes: fi.Length);

        //info = AssetProcessInfo.MakeDone<TextureManifestRecord>();
        return new TexturePayload(image.Data, desc, props, in fileSpec);
    }

    public CubeMapPayload LoadCubeMap(CubeMapDescriptor record)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(record.Textures.Length, 6);

        var faceData = new ReadOnlyMemory<byte>[6];
        var faceFiles = new AssetFileSpec[6];

        for (int i = 0; i < 6; i++)
        {
            var path = AssetPaths.GetCubeMapPath(record.Textures[i]);
            var fi = new FileInfo(path);
            if (!fi.Exists) throw new FileNotFoundException("File not found.", path);

            using var stream = File.OpenRead(path);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            ValidateImageResult(image, record);

            faceData[i] = image.Data;

            faceFiles[i] = new AssetFileSpec(
                storage: AssetStorageKind.FileSystem,
                logicalName: record.Name,
                relativePath: record.Textures[i],
                sizeBytes: fi.Length);
        }

        var desc = new GfxTextureDescriptor(
            width: record.Width,
            height: record.Height,
            kind: TextureKind.CubeMap,
            format: record.PixelFormat);

        var props = new GfxTextureProperties(
            preset: record.Preset,
            anisotropy: TextureAnisotropy.Off,
            lodBias: 0);

        return new CubeMapPayload(faceData, faceFiles, desc, props);
    }

    private static void ValidateImageResult(ImageResult result)
    {
        ArgumentNullException.ThrowIfNull(result, nameof(result));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Width, 0, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Height, 0, nameof(result.Height));
    }

    private static void ValidateImageResult(ImageResult result, CubeMapDescriptor record)
    {
        ValidateImageResult(result);
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Width, record.Width, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Height, record.Height, nameof(result.Height));
    }
}