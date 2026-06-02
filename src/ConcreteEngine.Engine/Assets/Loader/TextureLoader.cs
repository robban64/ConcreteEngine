using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets.Importer;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class TextureLoader(GfxTextures gfx) : AssetTypeLoader<Texture, TextureRecord>
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
        if (record.TextureKind == TextureKind.CubeMap)
            return LoadCubeMap(record, ctx);

        using var textureData = TextureImporter.LoadTexture(record, EnginePath.TexturePath, AssetRecord.GetDefaultFilename(record), out var meta);
        var textureId = gfx.CreateTexture2D(meta.Size, in meta.TextureProps, textureData.AsSpan());
        var texture = CreateTexture(ctx.Id, textureId, meta.Size, record);

        if (record.InMemory)
            texture.SetPixelData(textureData.AsSpan().ToArray());

        return texture;
    }

    protected override Texture LoadInMemory(TextureRecord record, LoaderContext ctx)
    {
        var texture = CreateTexture(ctx.Id, default, default, record);

        if (record.InMemory)
            texture.SetPixelData(TextureImporter.LoadInMemory(EnginePath.TexturePath, record, out _));

        return texture;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private Texture LoadCubeMap(TextureRecord record, LoaderContext ctx)
    {
        TextureId textureId = default;
        Size2D size = default;
        for (var i = 0; i < 6; i++)
        {
            var filename = record.Files[$"face:{i}"];
            using var textureData = TextureImporter.LoadTexture(record, EnginePath.TexturePath, filename, out var meta);
            if (textureId == default)
            {
                textureId = gfx.CreateCubeMap(meta.Size, in meta.TextureProps);
                size = meta.Size;
            }
            gfx.UploadCubeMapFace(textureId, textureData.AsSpan(), meta.Size, i);
        }

        return CreateTexture(ctx.Id, textureId, size, record);
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

        var textureId = gfx.CreateTexture2D(meta.Size, in meta.TextureProps, currentBlock.Data.AsSpan());

        var texture = new Texture(
            embedded.Name,
            textureId,
            meta.Size,
            new TextureProperties(
                lod: 0,
                kind: TextureKind.Texture2D,
                preset: embedded.Preset,
                anisotropy: anisotropy,
                pixelFormat: embedded.PixelFormat
            )
        ) { Id = assetId, GId = embedded.GId, Usage = embedded.SlotKind };

        embedded.PixelDataBlock = null;

        if (--_storedEmbeddedBlocks == 0) Allocator.Clear();

        return texture;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Texture CreateTexture(AssetId id, TextureId textureId, Size2D size, TextureRecord record,
        TextureUsage usage = TextureUsage.Albedo)
    {
        return new Texture(
            record.Name,
            textureId,
            size,
            new TextureProperties(
                lod: record.LodBias,
                kind: record.TextureKind,
                preset: record.Preset,
                anisotropy: record.Anisotropy,
                pixelFormat: record.PixelFormat
            )
        ) { Id = id, GId = record.GId, Usage = usage };
    }
}