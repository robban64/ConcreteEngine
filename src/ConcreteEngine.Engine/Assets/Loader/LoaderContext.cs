using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class LoaderContext
{
    public AssetId TargetId { get; init; }
    public IAssetDescriptor Descriptor { get; init; }
    public string FilePath { get; init; }
    public bool IsHotReload { get; init; }

    public List<AssetFileSpec> FileSpecs { get; } = [];
    public List<EmbeddedRecord> EmbeddedAssets { get; } = [];

    public LoaderContext(AssetId id, IAssetDescriptor descriptor, string path, bool hotReload)
    {
        TargetId = id;
        Descriptor = descriptor;
        FilePath = path;
        IsHotReload = hotReload;
    }
    
    public TDesc DescriptorAs<TDesc>() where TDesc : class, IAssetDescriptor 
        => (TDesc)Descriptor;
}