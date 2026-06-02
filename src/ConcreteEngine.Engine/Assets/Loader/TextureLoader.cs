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
    private readonly Dictionary<Guid, TextureData> _embeddedTextures = new(8);
    
    public int StoredEmbeddedCount => _embeddedTextures.Count;
    
    protected override void OnActivate()
    {
        if(_embeddedTextures.Count > 0)
            throw new InvalidOperationException("Embedded textures remains in the loader");
    }

    protected override void OnDeActivate()
    {
        if(_embeddedTextures.Count > 0)
            throw new InvalidOperationException("Embedded textures remains in the loader");
    }

    public unsafe void StoreEmbedded(Guid guid, byte* rawData, int length, TexturePixelFormat format, out Size2D size)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(guid, Guid.Empty);
        var textureData = TextureImporter.ImportUnmanagedTexture(rawData, length, format, out size);
        _embeddedTextures.Add(guid, new TextureData(guid, in textureData));
    }
    
    protected override Texture Load(TextureRecord record, LoaderContext ctx)
    {
        if (record.TextureKind == TextureKind.CubeMap)
            return LoadCubeMap(record, ctx);

        var filename = AssetRecord.GetDefaultFilename(record);
        var textureData = NativeArray<byte>.MakeNull();
        try
        {
            textureData = TextureImporter.LoadTexture(record, EnginePath.TexturePath, filename, out var size);
            var props = TextureImporter.CreateTextureProps(record);
            
            var textureId = gfx.CreateTexture2D(size, in props, textureData.AsSpan());
            var texture = CreateTexture(ctx.Id, textureId, size, record);
            
            if (record.InMemory) texture.SetPixelData( new TextureData(texture.GId, in textureData));
            
            return texture;
        }
        finally
        {
            if(!record.InMemory) textureData.Dispose();
        }


    }

    //?
    protected override Texture LoadInMemory(TextureRecord record, LoaderContext ctx)
    {
        throw new NotImplementedException();
        
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private Texture LoadCubeMap(TextureRecord record, LoaderContext ctx)
    {
        TextureId textureId = default;
        Size2D size = default;
        for (var i = 0; i < 6; i++)
        {
            var filename = record.Files[$"face:{i}"];
            using var data = TextureImporter.LoadTexture(record, EnginePath.TexturePath, filename, out var faceSize);
            if (textureId == default)
            {
                var props = TextureImporter.CreateTextureProps(record);
                textureId = gfx.CreateCubeMap(faceSize, in props);
                size = faceSize;
            }
            gfx.UploadCubeMapFace(textureId, data.AsSpan(), size, i);
        }

        return CreateTexture(ctx.Id, textureId, size, record);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Texture LoadEmbedded(AssetId assetId, EmbeddedSceneTexture embedded)
    {
        ArgumentNullException.ThrowIfNull(embedded.Name);
       
        if(!_embeddedTextures.TryGetValue(embedded.GId, out var entry))
            throw new InvalidOperationException($"Embedded texture '{embedded.Name}' not found");
        
        var anisotropy = embedded.SlotKind == TextureUsage.Albedo ? AnisotropyLevel.Default : AnisotropyLevel.Off;
        var props = new CreateTextureProps(0, TextureKind.Texture2D, embedded.PixelFormat, embedded.Preset,
            TextureImporter.GetAnisotropy(anisotropy));

        var textureId = gfx.CreateTexture2D(embedded.Dimensions, in props, entry.GetPixelData());

        var texture = new Texture(
            embedded.Name,
            textureId,
            embedded.Dimensions,
            new TextureProperties(
                lod: 0,
                kind: TextureKind.Texture2D,
                preset: embedded.Preset,
                anisotropy: anisotropy,
                pixelFormat: embedded.PixelFormat
            )
        ) { Id = assetId, GId = embedded.GId, Usage = embedded.SlotKind };
        
        _embeddedTextures.Remove(embedded.GId);
        entry.Dispose();
        
        return texture;
    }

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