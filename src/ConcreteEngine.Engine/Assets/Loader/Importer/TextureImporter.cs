using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using StbImageSharp;

namespace ConcreteEngine.Engine.Assets.Loader.Importer;

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
    public static ArenaBlockPtr ImportUnmanagedTexture(byte* data, ArenaAllocator allocator, int length,
        TexturePixelFormat format, out Size2D size)
    {
        using var stream = new UnmanagedMemoryStream(data, length);
        var ctx = new StbImage.stbi__context(stream);
        return WriteTexture(ctx, allocator, format, out size);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ArenaBlockPtr LoadTexture(TextureRecord record, string path, ArenaAllocator allocator,
        out TextureUploadMeta meta)
    {
        path = Path.Join(path, AssetRecord.GetDefaultFilename(record));
        if (!File.Exists(path)) throw new FileNotFoundException("File not found.", path);

        int x, y, comp;

        using var stream = File.OpenRead(path);
        var ctx = new StbImage.stbi__context(stream);
        var block = WriteTexture(ctx, allocator, record.PixelFormat, out var size);


        meta = CreateMeta(size, record.PixelFormat, TextureKind.Texture2D, record.Preset,
            GetAnisotropy(record.Anisotropy), record.LodBias);

        return block;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ArenaBlockPtr LoadCubeMap(TextureRecord record, string basePath, ArenaAllocator allocator,
        out TextureUploadMeta meta)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(record.Files.Count, 6);

        var size = Size2D.Zero;
        ArenaBlockPtr startBlock = null;

        for (int i = 0; i < 6; i++)
        {
            var path = Path.Join(basePath, record.Files[$"face:{i}"]);
            if (!File.Exists(path)) throw new FileNotFoundException("File not found.", path);

            using var stream = File.OpenRead(path);
            var ctx = new StbImage.stbi__context(stream);
            var block = WriteTexture(ctx, allocator, record.PixelFormat, out var textureSize);

            if (i == 0)
            {
                size = textureSize;
                startBlock = block;
            }
        }

        if (startBlock.IsNull) throw new InvalidOperationException("StartBlock is null");

        meta = CreateMeta(size, record.PixelFormat, TextureKind.CubeMap, record.Preset, TextureAnisotropy.Off, 0);
        return startBlock;
    }

    //

    private static ArenaBlockPtr WriteTexture(
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
        NativeMemory.Copy(imageData, block.DataPtr, (nuint)sizeInBytes);
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

    private static void ValidateImageResult(ImageResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Width, 0, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Height, 0, nameof(result.Height));
    }
}