using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using StbImageSharp;

namespace ConcreteEngine.Engine.Assets.Loader.Importer;

internal static class TextureImporter
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static unsafe byte[] ImportUnmanagedTexture(byte* data, int length, int width, int height, TexturePixelFormat format, out Size2D dimension)
    {
        using var stream = new UnmanagedMemoryStream(data, length);
        var image = ImageResult.FromStream(stream, GetColorComponent(format));
        ValidateImageResult(image);

        if (width != length && height != 0)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(image.Width, width, nameof(width));
            ArgumentOutOfRangeException.ThrowIfNotEqual(image.Height, height, nameof(height));
        }

        dimension = new Size2D(image.Width, image.Height);
        return image.Data;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
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
    public static TextureUploadMeta CreateMeta(Size2D size, TexturePixelFormat format, TextureKind kind,
        TexturePreset preset, TextureAnisotropy anisotropy, float lodBias)
    {
        var desc = new CreateTextureInfo(size.Width, size.Height, kind, format);
        var props = new CreateTextureProps(lodBias, preset, anisotropy);
        return new TextureUploadMeta(desc, props);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static TextureAnisotropy GetAnisotropy(AnisotropyLevel format)
    {
        return format switch
        {
            AnisotropyLevel.Default => EngineSettings.Instance.Graphics.MaxAnisotropy,
            AnisotropyLevel.Off => TextureAnisotropy.Off,
            AnisotropyLevel.X2 => TextureAnisotropy.X2,
            AnisotropyLevel.X4 => TextureAnisotropy.X4,
            AnisotropyLevel.X8 => TextureAnisotropy.X8,
            AnisotropyLevel.X16 => TextureAnisotropy.X16,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateImageResult(ImageResult result, Size2D size)
    {
        ValidateImageResult(result);
        if (size.IsNegativeOrZero()) throw new ArgumentNullException(nameof(size));
        ArgumentOutOfRangeException.ThrowIfNotEqual(size.Width, result.Width, nameof(size.Width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(size.Width, result.Width, nameof(size.Height));
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateImageResult(ImageResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Width, 0, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Height, 0, nameof(result.Height));
    }


}