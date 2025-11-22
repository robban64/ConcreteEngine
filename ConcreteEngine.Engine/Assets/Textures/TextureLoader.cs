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
    public TextureImportResult LoadEmbeddedTexture(byte[] data, int width, int height, TextureDescriptor record)
    {
        var image = ImageResult.FromMemory(data, GetColorComponent(record.PixelFormat));
        ValidateImageResult(image);

        ArgumentOutOfRangeException.ThrowIfNotEqual(image.Width, width, nameof(image.Width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(image.Width, height, nameof(image.Width));


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
            Storage: AssetStorageKind.Embedded,
            LogicalName: record.Name,
            RelativePath: record.Filename,
            SizeBytes: data.Length);

        var meta = new TextureUploadMeta(desc, props);

        uploader.UploadTexture(image.Data, meta, out var info);

        return new TextureImportResult
        {
            Data = null,
            FileSpec = fileSpec,
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
            Storage: AssetStorageKind.FileSystem,
            LogicalName: record.Name,
            RelativePath: record.Filename,
            SizeBytes: fi.Length);

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
                Storage: AssetStorageKind.FileSystem,
                LogicalName: record.Name,
                RelativePath: record.Textures[i],
                SizeBytes: fi.Length);
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