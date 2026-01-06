using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Renderer.Definitions;
using StbImageSharp;

namespace ConcreteEngine.Engine.Assets.Textures;

internal static class TextureLoader
{
    public static ReadOnlyMemory<byte> LoadEmbeddedTexture(TextureEmbeddedRecord record, out TextureUploadMeta meta)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(record.PixelData);
        ArgumentOutOfRangeException.ThrowIfLessThan(record.PixelData.Length, 4);

        var image = ImageResult.FromMemory(record.PixelData, GetColorComponent(record.PixelFormat));
        ValidateImageResult(image);

        if (record.Width != record.PixelData.Length && record.Height != 0)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(image.Width, record.Width, nameof(image.Width));
            ArgumentOutOfRangeException.ThrowIfNotEqual(image.Width, record.Height, nameof(image.Width));
        }

        var settings = EngineSettings.Instance.Graphics;

        var desc = new CreateTextureInfo(
            width: image.Width,
            height: image.Height,
            kind: TextureKind.Texture2D,
            format: record.PixelFormat
        );

        var props = new CreateTextureProps(
            preset: TexturePreset.LinearMipmapRepeat,
            anisotropy: record.SlotKind == TextureSlotKind.Albedo ? settings.MaxAnisotropy : TextureAnisotropy.Off,
            lodBias: 0
        );

         meta = new TextureUploadMeta(desc, props);
         return image.Data;
    }

    public static ReadOnlyMemory<byte> LoadTexture(string filePath, TextureRecord record, out TextureUploadMeta meta)
    {
        //StbImage.stbi_set_flip_vertically_on_load(1);

        var path = Path.Combine(filePath, AssetRecord.GetDefaultFilename(record));
        if (File.Exists(filePath)) throw new FileNotFoundException("File not found.", filePath);

        using var stream = File.OpenRead(path);


        var image = ImageResult.FromStream(stream, GetColorComponent(record.PixelFormat));
        ValidateImageResult(image);

        var desc = new CreateTextureInfo(
            width: image.Width,
            height: image.Height,
            kind: TextureKind.Texture2D,
            format: record.PixelFormat
        );

        var props = new CreateTextureProps(
            preset: record.Preset,
            anisotropy: GetAnisotropy(record.Anisotropy),
            lodBias: record.LodBias
        );

         meta = new TextureUploadMeta(desc, props);
         return image.Data;
    }

    public static ReadOnlyMemory<byte>[] LoadCubeMap(string filePath, TextureRecord record, out TextureUploadMeta meta)
    {

        ArgumentOutOfRangeException.ThrowIfNotEqual(record.Files.Count, 6);

        var faceData = new ReadOnlyMemory<byte>[6];
        var faceFiles = new AssetFileSpec[6];

        int width = 0, height = 0;
        for (int i = 0; i < 6; i++)
        {
            var path = Path.Combine(filePath, record.Files[$"face:{i}"]);

            var fi = new FileInfo(path);
            if (!fi.Exists) throw new FileNotFoundException("File not found.", path);

            using var stream = File.OpenRead(path);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            width = image.Width;
            height = image.Height;
            ValidateImageResult(image, width, height);

            faceData[i] = image.Data;
        }

        var desc = new CreateTextureInfo(
            width: width,
            height: height,
            kind: TextureKind.CubeMap,
            format: record.PixelFormat);

        var props = new CreateTextureProps(
            preset: record.Preset,
            anisotropy: TextureAnisotropy.Off,
            lodBias: 0);


        meta = new TextureUploadMeta(desc, props);
        return faceData;
    }

    private static TextureAnisotropy GetAnisotropy(TextureAnisotropyProfile format)
    {
        return format switch
        {
            TextureAnisotropyProfile.Default => EngineSettings.Instance.Graphics.MaxAnisotropy,
            TextureAnisotropyProfile.Off => TextureAnisotropy.Off,
            TextureAnisotropyProfile.X2 => TextureAnisotropy.X2,
            TextureAnisotropyProfile.X4 => TextureAnisotropy.X4,
            TextureAnisotropyProfile.X8 => TextureAnisotropy.X8,
            TextureAnisotropyProfile.X16 => TextureAnisotropy.X16,
            _ => throw new ArgumentOutOfRangeException()
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

    private static void ValidateImageResult(ImageResult result, int width, int height)
    {
        ValidateImageResult(result);
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Width, width, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Height, height, nameof(result.Height));
    }
}