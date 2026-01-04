using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Loader;

internal abstract class AssetTypeLoader<TAsset> : IDisposable 
    where TAsset : AssetObject
{
    // Configuration
    public AssetKind Kind = AssetEnums.ToAssetKind<TAsset>();
    
    protected abstract string[] SupportedExtensions { get; }

    public virtual bool CanLoad(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return SupportedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase); 
    }

    public TAsset Load(LoaderContext ctx)
    {
        AddFileSpec(ctx, ctx.FilePath, logicalName: ctx.Descriptor.Name);

        var asset = LoadInternal(ctx);

        if (ctx.EmbeddedAssets.Count > 0)
        {
            ctx.EmbeddedAssets.Sort();
        }

        return asset;    }

    protected abstract TAsset LoadInternal(LoaderContext context);

    protected void AddFileSpec(LoaderContext ctx, string filePath, string? logicalName = null)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists) throw new FileNotFoundException($"Asset source missing: {filePath}");

        var name = logicalName ?? Path.GetFileNameWithoutExtension(info.Name);
        var relPath = Path.GetRelativePath(Environment.CurrentDirectory, info.FullName);
        var fileId = new AssetFileId(relPath.GetHashCode()); 

        var spec = new AssetFileSpec(
            Id: fileId,
            GId: Guid.NewGuid(),
            Storage: AssetStorageKind.FileSystem,
            LogicalName: name,
            RelativePath: relPath,
            SizeBytes: info.Length,
            ContentHash: null,
            Source: null
        );

        ctx.FileSpecs.Add(spec);
    }
    
    protected void AddEmbedded<TEmbedded>(LoaderContext ctx, TEmbedded record) 
        where TEmbedded : EmbeddedRecord
    {
        ctx.EmbeddedAssets.Add(record);
    }

    public virtual void Dispose() { }
}