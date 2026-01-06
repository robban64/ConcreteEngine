using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using StbImageSharp;

namespace ConcreteEngine.Engine.Assets.Loader.Importer;

internal static class TextureImporter
{
    public static ReadOnlyMemory<byte> LoadEmbeddedTexture(TextureEmbeddedRecord record, out TextureUploadMeta meta)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(record.PixelData);
        ArgumentOutOfRangeException.ThrowIfLessThan(record.PixelData.Length, 4);

        var image = ImageResult.FromMemory(record.PixelData, GetColorComponent(record.PixelFormat));
        var size = new Size2D(image.Width, image.Height);
        ValidateImageResult(image);

        if (record.Width != record.PixelData.Length && record.Height != 0)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(image.Width, record.Width, nameof(image.Width));
            ArgumentOutOfRangeException.ThrowIfNotEqual(image.Height, record.Height, nameof(image.Height));
        }

        var settings = EngineSettings.Instance.Graphics;
        var anisotropy = record.SlotKind == MaterialSlotKind.Albedo ? settings.MaxAnisotropy : TextureAnisotropy.Off;

        meta = CreateMeta(size, record.PixelFormat, TextureKind.Texture2D, TexturePreset.LinearMipmapRepeat, anisotropy,
            0);
        return image.Data;
    }

    public static ReadOnlyMemory<byte> LoadTexture(string filePath, TextureRecord record, out TextureUploadMeta meta)
    {
        var path = Path.Combine(filePath, AssetRecord.GetDefaultFilename(record));
        if (!File.Exists(path)) throw new FileNotFoundException("File not found.", path);

        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, GetColorComponent(record.PixelFormat));
        var size = new Size2D(image.Width, image.Height);
        ValidateImageResult(image);

        meta = CreateMeta(size, record.PixelFormat, TextureKind.Texture2D, record.Preset,
            GetAnisotropy(record.Anisotropy), record.LodBias);
        return image.Data;
    }

    public static ReadOnlyMemory<byte>[] LoadCubeMap(string filePath, TextureRecord record, out TextureUploadMeta meta)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(record.Files.Count, 6);

        var faceData = new ReadOnlyMemory<byte>[6];
        var size = Size2D.Zero;

        for (int i = 0; i < 6; i++)
        {
            var path = Path.Combine(filePath, record.Files[$"face:{i}"]);
            if (!File.Exists(path)) throw new FileNotFoundException("File not found.", path);

            using var stream = File.OpenRead(path);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            if (i == 0)
                size = new Size2D(image.Width, image.Height);

            ValidateImageResult(image, size);

            faceData[i] = image.Data;
        }

        meta = CreateMeta(size, record.PixelFormat, TextureKind.CubeMap, record.Preset, TextureAnisotropy.Off,
            0);
        return faceData;
    }

    // --- Helpers ---

    private static TextureUploadMeta CreateMeta(Size2D size, TexturePixelFormat format, TextureKind kind,
        TexturePreset preset, TextureAnisotropy anisotropy, float lodBias)
    {
        var desc = new CreateTextureInfo(size.Width, size.Height, kind, format);
        var props = new CreateTextureProps(lodBias, preset, anisotropy);
        return new TextureUploadMeta(desc, props);
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
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Width, 0, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Height, 0, nameof(result.Height));
    }

    private static void ValidateImageResult(ImageResult result, Size2D size)
    {
        ValidateImageResult(result);
        if (size.IsNegativeOrZero()) throw new ArgumentNullException(nameof(size));
        ArgumentOutOfRangeException.ThrowIfNotEqual(size.Width, result.Width, nameof(size.Width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(size.Width, result.Width, nameof(size.Height));
    }
}