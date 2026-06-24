using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using StbImageSharp;

namespace ConcreteEngine.Engine.Assets.Importer;

internal static unsafe class TextureImporter
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static NativeArray<byte> ImportUnmanagedTexture(byte* data, int length,
        TexturePixelFormat format, out Size2D size)
    {
        using var stream = new UnmanagedMemoryStream(data, length);
        var ctx = new StbImage.stbi__context(stream);

        int x, y, comp;
        var imageData = StbImage.stbi__load_and_postprocess_8bit(ctx, &x, &y, &comp, (int)GetColorComponent(format));

        if (imageData == null)
            throw new InvalidOperationException(StbImage.stbi__g_failure_reason);

        size = new Size2D(x, y);
        var sizeInBytes = x * y * 4;

        return NativeArray.From(imageData, sizeInBytes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static NativeArray<byte> LoadTexture(TextureRecord record, string filePath, out Size2D size)
    {
        int x, y, comp;
        using var stream = File.OpenRead(filePath);
        var ctx = new StbImage.stbi__context(stream);
        var imageData =
            StbImage.stbi__load_and_postprocess_8bit(ctx, &x, &y, &comp, (int)GetColorComponent(record.PixelFormat));

        if (imageData == null)
            throw new InvalidOperationException(StbImage.stbi__g_failure_reason);

        var sizeInBytes = x * y * 4;

        size = new Size2D(x, y);

        return NativeArray.From(imageData, sizeInBytes);
    }

    //

    public static CreateTextureProps CreateTextureProps(TextureRecord record)
    {
        return new CreateTextureProps(record.LodBias, record.TextureKind, record.PixelFormat, record.Preset,
            GetAnisotropy(record.Anisotropy));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static TextureAnisotropy GetAnisotropy(AnisotropyLevel format)
    {
        return format switch
        {
            AnisotropyLevel.Default => EngineSettings.Current.Graphics.MaxAnisotropy,
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

    private static void ValidateImageResult(ImageResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Width, 0, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Height, 0, nameof(result.Height));
    }
}