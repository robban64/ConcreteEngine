#region

using System.Text.Json.Serialization;
using ConcreteEngine.Core.Assets.Data;

#endregion

namespace ConcreteEngine.Core.Assets.Descriptors;

public interface IAssetCatalog
{
    [JsonIgnore] int Count { get; }

    [JsonIgnore] IReadOnlyList<IAssetDescriptor> Records { get; }
}

public interface IAssetDescriptor
{
    string Name { get; }
    AssetKind Kind { get; }
    AssetLoadingMode LoadMode { get; }
}

public interface IAssetData
{
    public AssetId AssetId { get; }
    public string Name { get; }
    public AssetKind Kind { get; }
}