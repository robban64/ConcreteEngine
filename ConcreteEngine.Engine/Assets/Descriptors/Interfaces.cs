using System.Text.Json.Serialization;
using ConcreteEngine.Engine.Assets.Data;

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
    AssetId AssetId { get; }
    string Name { get; }
    AssetKind Kind { get; }
}