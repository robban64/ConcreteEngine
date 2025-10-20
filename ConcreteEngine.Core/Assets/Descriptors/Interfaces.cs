#region

using System.Text.Json.Serialization;
using ConcreteEngine.Core.Assets.Data;

#endregion

namespace ConcreteEngine.Core.Assets.Descriptors;

public interface IAssetDescriptor
{
    string Name { get; }
    AssetKind Kind { get; }
}

public interface IAssetCatalog
{
    [JsonIgnore] int Count { get; }

    [JsonIgnore] IReadOnlyList<IAssetDescriptor> Records { get; }
}