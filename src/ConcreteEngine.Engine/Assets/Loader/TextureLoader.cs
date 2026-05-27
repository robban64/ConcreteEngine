using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets.Importer;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class TextureLoader(AssetGfxUploader uploader) : AssetTypeLoader<Texture, TextureRecord>(uploader)
{
    private const int SizeHigh = 1024 * 1024 * 48;
    private const int SizeLow = 1024 * 1024 * 24;

    protected override int SetupAllocSize => SizeHigh;
    protected override int DefaultAllocSize => SizeLow;

    private int _storedEmbeddedBlocks;

    public unsafe MemoryBlockPtr StoreEmbedded(byte* data, int length, TexturePixelFormat format, out Size2D size)
    {
        var block = TextureImporter.ImportUnmanagedTexture(data, Allocator, length, format, out size);
        _storedEmbeddedBlocks++;
        return block;
    }

    public void ClearEmbedded()
    {
        _storedEmbeddedBlocks = 0;
        Allocator.Clear();
    }

    protected override void OnSetup()
    {
        _storedEmbeddedBlocks = 0;
    }

    protected override void OnTeardown()
    {
        _storedEmbeddedBlocks = 0;
    }


    protected override Texture Load(TextureRecord record, LoaderContext ctx)
    {
        if (_storedEmbeddedBlocks > 0)
            throw new InvalidOperationException("Cannot Load when embedded blocks already stored");

        Allocator.Clear();

        if (record.TextureKind == TextureKind.CubeMap)
            return LoadCubeMap(record, ctx);

        var block = TextureImporter.LoadTexture(record, EnginePath.TexturePath, Allocator, out var meta);
        Uploader.UploadTexture(block.Data.AsSpan(), in meta, out var result);
        var texture = CreateTexture(ctx.Id, record, result);

        if (record.InMemory)
            texture.SetPixelData(block.Data.AsSpan().ToArray());

        Allocator.Clear();
        return texture;
    }

    protected override Texture LoadInMemory(TextureRecord record, LoaderContext ctx)
    {
        var texture = CreateTexture(ctx.Id, record, default);

        if (record.InMemory)
            texture.SetPixelData(TextureImporter.LoadInMemory(EnginePath.TexturePath, record, out _));

        return texture;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private unsafe Texture LoadCubeMap(TextureRecord record, LoaderContext ctx)
    {
        var block = TextureImporter.LoadCubeMap(record, EnginePath.TexturePath, Allocator, out var meta);

        var data = stackalloc NativeView<byte>[6];
        var currentBlock = block;
        for (var i = 0; i < 6; i++)
        {
            if (currentBlock.IsNull)
                throw new InvalidOperationException($"CubeMap face {i} block is null");

            data[i] = currentBlock.Data;
            currentBlock = currentBlock.Next;
        }

        Uploader.UploadCubeMap(data, in meta, out var result);

        return CreateTexture(ctx.Id, record, result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Texture LoadEmbedded(AssetId assetId, EmbeddedSceneTexture embedded)
    {
        ArgumentNullException.ThrowIfNull(embedded.Name);
        if (embedded.PixelDataBlock.IsNull)
            throw new ArgumentNullException(nameof(embedded.PixelDataBlock));

        var currentBlock = Allocator.GetHead();

        if (_storedEmbeddedBlocks <= 0 || currentBlock.IsNull)
            throw new InvalidOperationException("No embedded blocks registered");

        while (currentBlock != embedded.PixelDataBlock && !currentBlock.IsNull)
            currentBlock = currentBlock.Next;

        if (currentBlock != embedded.PixelDataBlock || currentBlock.IsNull)
            throw new InvalidOperationException($"Block not found for embedded texture '{embedded.Name}'");

        var anisotropy = embedded.SlotKind == TextureUsage.Albedo ? AnisotropyLevel.Default : AnisotropyLevel.Off;
        var meta = TextureImporter.CreateMeta(embedded.Dimensions, embedded.PixelFormat, TextureKind.Texture2D,
            embedded.Preset, TextureImporter.GetAnisotropy(anisotropy), 0);

        Uploader.UploadTexture(currentBlock.Data.AsSpan(), in meta, out var result);

        var texture = new Texture(
            embedded.Name,
            result.TextureId,
            result.Size,
            new TextureProperties(
                lodBias: 0,
                mipLevels: 0,
                kind: TextureKind.Texture2D,
                Usage: embedded.SlotKind,
                preset: embedded.Preset,
                anisotropy: anisotropy,
                pixelFormat: embedded.PixelFormat
            )
        ) { Id = assetId, GId = embedded.GId };

        embedded.PixelDataBlock = null;

        if (--_storedEmbeddedBlocks == 0) Allocator.Clear();

        return texture;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Texture CreateTexture(AssetId id, TextureRecord record, TextureCreationInfo result,
        TextureUsage usage = TextureUsage.Albedo)
    {
        return new Texture(
            record.Name,
            result.TextureId,
            result.Size,
            new TextureProperties(
                lodBias: record.LodBias,
                mipLevels: 0,
                kind: record.TextureKind,
                Usage: usage,
                preset: record.Preset,
                anisotropy: record.Anisotropy,
                pixelFormat: record.PixelFormat
            )
        ) { Id = id, GId = record.GId };
    }
}