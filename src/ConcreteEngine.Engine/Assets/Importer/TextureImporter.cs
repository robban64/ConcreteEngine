using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Graphics.Gfx;
using StbImageSharp;

namespace ConcreteEngine.Engine.Assets.Importer;

internal static unsafe class TextureImporter
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ReadOnlyMemory<byte> LoadInMemory(string filePath, TextureRecord record, out TextureUploadMeta meta)
    {
        var path = Path.Join(filePath, AssetRecord.GetDefaultFilename(record));
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
    public static MemoryBlockPtr ImportUnmanagedTexture(byte* data, ArenaAllocator allocator, int length,
        TexturePixelFormat format, out Size2D size)
    {
        using var stream = new UnmanagedMemoryStream(data, length);
        var ctx = new StbImage.stbi__context(stream);
        return WriteTexture(ctx, allocator, format, out size);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ScopedNativeMemory LoadTexture(TextureRecord record, string path, string filename, out TextureUploadMeta meta)
    {
        path = Path.Join(path, filename);
        if (!File.Exists(path)) throw new FileNotFoundException("File not found.", path);

        int x, y, comp;

        using var stream = File.OpenRead(path);
        var ctx = new StbImage.stbi__context(stream);
        var imageData = StbImage.stbi__load_and_postprocess_8bit(ctx, &x, &y, &comp, (int)GetColorComponent(record.PixelFormat));

        if (imageData == null)
            throw new InvalidOperationException(StbImage.stbi__g_failure_reason);

        var size = new Size2D(x, y);
        var sizeInBytes = x * y * 4;

        meta = CreateMeta(size, record.PixelFormat, record.TextureKind, record.Preset,
            GetAnisotropy(record.Anisotropy), record.LodBias);

        return new ScopedNativeMemory(imageData, sizeInBytes);
    }

    //

    private static MemoryBlockPtr WriteTexture(
        StbImage.stbi__context ctx,
        ArenaAllocator allocator,
        TexturePixelFormat format,
        out Size2D size)
    {
        int x, y, comp;
        var imageData = StbImage.stbi__load_and_postprocess_8bit(ctx, &x, &y, &comp, (int)GetColorComponent(format));

        if (imageData == null)
            throw new InvalidOperationException(StbImage.stbi__g_failure_reason);

        size = new Size2D(x, y);
        var sizeInBytes = x * y * 4;

        var block = allocator.Alloc(sizeInBytes);
        NativeMemory.Copy(imageData, block.Data, (nuint)sizeInBytes);
        NativeMemory.Free(imageData);
        return block;
    }

    public static TextureUploadMeta CreateMeta(
        Size2D size,
        TexturePixelFormat format,
        TextureKind kind,
        TexturePreset preset,
        TextureAnisotropy anisotropy,
        float lodBias)
    {
        var props = new CreateTextureProps(lodBias, kind, format, preset, anisotropy);
        return new TextureUploadMeta(size.ToSize3D(1), props);
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