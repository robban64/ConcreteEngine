using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets.Importer;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class TextureLoader(GfxTextures gfx) : AssetTypeLoader<Texture, TextureRecord>
{
    private readonly Dictionary<Guid, TextureData> _embeddedTextures = new(8);

    public int StoredEmbeddedCount => _embeddedTextures.Count;

    protected override void OnActivate()
    {
        if (_embeddedTextures.Count > 0)
            throw new InvalidOperationException("Embedded textures remains in the loader");
    }

    protected override void OnDeActivate()
    {
        if (_embeddedTextures.Count > 0)
            throw new InvalidOperationException("Embedded textures remains in the loader");
    }

    public unsafe void StoreEmbedded(Guid guid, byte* rawData, int length, TexturePixelFormat format, out Size2D size)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(guid, Guid.Empty);
        var textureData = TextureImporter.ImportUnmanagedTexture(rawData, length, format, out size);
        _embeddedTextures.Add(guid, new TextureData(guid, in textureData));
    }

    protected override Texture Load(TextureRecord record, ImportContext ctx)
    {
        if (record.TextureKind == TextureKind.CubeMap)
            return LoadCubeMap(record, ctx);

        //var filename = AssetRecord.GetDefaultFilename(record);
        var filePath = ctx.GetFile(1).RelativePath;
        var textureData = NativeArray<byte>.MakeNull();
        try
        {
            textureData = TextureImporter.LoadTexture(record, filePath, out var size);

            var textureId = gfx.CreateTexture2D(textureData.AsReadOnlySpan(), size, record.PixelFormat);
            var texture = CreateTextureObject(ctx.Id, textureId, size, record);

            if (record.InMemory) texture.SetPixelData(new TextureData(texture.GId, in textureData));

            return texture;
        }
        finally
        {
            if (!record.InMemory) textureData.Dispose();
        }
    }

    //?
    protected override Texture LoadInMemory(TextureRecord record, ImportContext ctx)
    {
        throw new NotImplementedException();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private Texture LoadCubeMap(TextureRecord record, ImportContext ctx)
    {
        Size2D size = default;
        TextureId textureId = default;
        for (var i = 0; i < 6; i++)
        {
            var filePath = ctx.GetFile(i + 1).RelativePath;

            using var data = TextureImporter.LoadTexture(record, filePath, out var faceSize);
            if (textureId == default)
            {
                textureId = gfx.CreateCubeMap(faceSize, record.PixelFormat);
                size = faceSize;
            }

            gfx.UploadCubeMapFace(textureId, data.AsSpan(), size, i);
        }

        return CreateTextureObject(ctx.Id, textureId, size, record);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Texture LoadEmbedded(AssetId assetId, EmbeddedSceneTexture embedded)
    {
        ArgumentNullException.ThrowIfNull(embedded.Name);

        if (!_embeddedTextures.TryGetValue(embedded.GId, out var entry))
            throw new InvalidOperationException($"Embedded texture '{embedded.Name}' not found");

        var textureId = gfx.CreateTexture2D(entry.GetPixelData(), embedded.Dimensions, embedded.PixelFormat);

        
        var anisotropy = embedded.SlotKind == TextureUsage.Albedo
            ? EngineSettings.Current.Graphics.MaxAnisotropy
            : TextureAnisotropy.Off;

        var sampler = SamplerProfile.TrilinearClamp;
        if (anisotropy != TextureAnisotropy.Off)
            sampler = SamplerProfile.AnisotropicClamp;

        var texture = new Texture(
            name: embedded.Name,
            id: assetId,
            gid: embedded.GId,
            gfxId: textureId,
            size: embedded.Dimensions,
            profile: sampler,
            textureKind: TextureKind.Texture2D,
            pixelFormat: embedded.PixelFormat
        );

        _embeddedTextures.Remove(embedded.GId);
        entry.Dispose();

        return texture;
    }

    private static Texture CreateTextureObject(AssetId id, TextureId textureId, Size2D size, TextureRecord record)
    {
        var profile = SamplerProfile.TrilinearWrap;
        if (record.Profile is { } recordProfile) profile = recordProfile;

        return new Texture(
            name: record.Name,
            id: id,
            gid: record.Id,
            gfxId: textureId,
            size: size,
            profile: profile,
            textureKind: record.TextureKind,
            pixelFormat: record.PixelFormat
        );
    }
}