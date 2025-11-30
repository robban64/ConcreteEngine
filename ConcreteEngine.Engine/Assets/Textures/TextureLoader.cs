#region

using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.IO;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using StbImageSharp;

#endregion

namespace ConcreteEngine.Engine.Assets.Textures;

internal sealed class TextureLoader(AssetGfxUploader uploader)
{
    public TextureImportResult LoadEmbeddedTexture(TextureEmbeddedDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(descriptor.PixelData);
        ArgumentOutOfRangeException.ThrowIfLessThan(descriptor.PixelData.Length, 4);

        var image = ImageResult.FromMemory(descriptor.PixelData, GetColorComponent(descriptor.PixelFormat));
        ValidateImageResult(image);

        if (descriptor.Width != descriptor.PixelData.Length && descriptor.Height != 0)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(image.Width, descriptor.Width, nameof(image.Width));
            ArgumentOutOfRangeException.ThrowIfNotEqual(image.Width, descriptor.Height, nameof(image.Width));
        }


        var desc = new GfxTextureDescriptor(
            width: image.Width,
            height: image.Height,
            kind: TextureKind.Texture2D,
            format: descriptor.PixelFormat
        );

        var props = new GfxTextureProperties(
            preset: TexturePreset.LinearMipmapRepeat,
            anisotropy: TextureAnisotropy.X4,
            lodBias: 0
        );

        var meta = new TextureUploadMeta(desc, props);

        uploader.UploadTexture(image.Data, meta, out var info);

        return new TextureImportResult
        {
            Data = null,
            FileSpec = null!,
            CreationInfo = info,
            TextureDesc = desc,
            TextureProps = props
        };
    }

    public TextureImportResult LoadTexture(TextureDescriptor record)
    {
        //StbImage.stbi_set_flip_vertically_on_load(1);

        var path = AssetPaths.GetTexturePath(record.Filename);

        var fi = new FileInfo(path);
        if (!fi.Exists) throw new FileNotFoundException("File not found.", path);

        using var stream = File.OpenRead(path);


        var image = ImageResult.FromStream(stream, GetColorComponent(record.PixelFormat));
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

        var meta = new TextureUploadMeta(desc, props);
        uploader.UploadTexture(image.Data, meta, out var info);

        return new TextureImportResult
        {
            Data = record.InMemory ? image.Data : null,
            FileSpec = fileSpec,
            CreationInfo = info,
            TextureDesc = desc,
            TextureProps = props
        };
    }

    public CubeMapImportResult LoadCubeMap(CubeMapDescriptor record)
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


        var payload = new TextureUploadMeta(desc, props);
        uploader.UploadCubeMap(faceData, payload, out var info);


        return new CubeMapImportResult
        {
            FaceFiles = faceFiles, CreationInfo = info, TextureDesc = desc, TextureProps = props
        };
    }

    private static ColorComponents GetColorComponent(TexturePixelFormat format)
    {
        return format switch
        {
            TexturePixelFormat.Rgb => ColorComponents.RedGreenBlueAlpha,
            TexturePixelFormat.Rgba => ColorComponents.RedGreenBlueAlpha,
            TexturePixelFormat.SrgbAlpha => ColorComponents.RedGreenBlueAlpha,
            TexturePixelFormat.Depth => ColorComponents.Grey,
            TexturePixelFormat.Red => ColorComponents.Grey,
            _ => throw new ArgumentOutOfRangeException()
        };
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