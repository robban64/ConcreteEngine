using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Textures;

internal sealed class TextureLoaderModule
{
    private TextureLoader _loader;

    public TextureLoaderModule(AssetGfxUploader uploader)
    {
        _loader = new TextureLoader(uploader);
    }

    public Texture2D LoadEmbeddedTexture(AssetId id, TextureEmbeddedRecord record, AssetStore assetStore)
    {
        var result = _loader.LoadEmbeddedTexture(record);

        var texture = new Texture2D
        {
            Id = id,
            GId = record.GId,
            Name = record.EmbeddedName,
            ResourceId = result.CreationInfo.TextureId,
            Width = result.CreationInfo.Width,
            Height = result.CreationInfo.Height,
            IsCoreAsset = false,
            SlotKind = record.SlotKind
        };

        return texture;
    }

    public Texture2D LoadTexture2D(TextureDescriptor manifest, ref LoadAssetContext ctx)
    {
        if (manifest.MultiFilenames is not null)
            return LoadCubeMap(manifest, ref ctx);
        
        var result = _loader.LoadTexture(manifest);
        var args = ctx.GetFileArgs();

        ctx.FileSpecs =
        [
            new AssetFileSpec(
                Id: args.Id,
                GId: args.GId,
                Storage: AssetStorageKind.FileSystem,
                LogicalName: manifest.Name,
                RelativePath: manifest.Filename,
                SizeBytes: result.FileSize)
        ];

        var texture = new Texture2D
        {
            Id = ctx.Id,
            GId = ctx.GId,
            Name = manifest.Name,
            ResourceId = result.CreationInfo.TextureId,
            Width = result.CreationInfo.Width,
            Height = result.CreationInfo.Height,
            IsCoreAsset = ctx.IsCore
        };

        if (result.Data is { } tData)
            texture.SetPixelData(tData);

        return texture;
    }

    public Texture2D LoadCubeMap(TextureDescriptor manifest, ref LoadAssetContext ctx)
    {
        Span<FileSpecArgs> args = stackalloc FileSpecArgs[manifest.MultiFilenames!.Length];
        for(int i = 0; i < manifest.MultiFilenames.Length; i++)
            args[i] = ctx.GetFileArgs();
        
        var result = _loader.LoadCubeMap(manifest, args);
        ctx.FileSpecs = result.FaceFiles;

        return new Texture2D
        {
            Id = ctx.Id,
            GId = ctx.GId,
            Name = manifest.Name,
            ResourceId = result.CreationInfo.TextureId,
            Width = result.CreationInfo.Size,
            Height = result.CreationInfo.Size,
            IsCoreAsset = ctx.IsCore
        };
    }


    public void Unload()
    {
        _loader = null!;
    }
}