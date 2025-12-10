#region

using System.Text.Json.Serialization;
using ConcreteEngine.Engine.Assets.Data;

#endregion

namespace ConcreteEngine.Engine.Assets.Descriptors;

internal interface IAssetCatalog
{
    [JsonIgnore] int Count { get; }

    [JsonIgnore] IReadOnlyList<IAssetDescriptor> Records { get; }
}

internal interface IAssetDescriptor
{
    string Name { get; }
    AssetKind Kind { get; }
    AssetLoadingMode LoadMode { get; }
}

internal interface IAssetData
{
    public AssetId AssetId { get; }
    public string Name { get; }
    public AssetKind Kind { get; }
}