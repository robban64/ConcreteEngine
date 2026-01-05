using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets;

public interface IAssetList
{
    int Count { get; }
    int FileCount { get; }
    AssetKind Kind { get; }
    AssetStoreMeta ToSnapshot();
}

internal sealed class AssetList<T>(AssetKind kind) : IAssetList where T : AssetObject
{
    internal List<T> Asset { get; } = [];
    public int FileCount { get; internal set; }
    public int Count => Asset.Count;
    public AssetKind Kind => kind;

    internal ReadOnlySpan<T> AssetSpan => CollectionsMarshal.AsSpan(Asset);

    public void Add(T asset, int fileSpecs)
    {
        FileCount += fileSpecs;
        Asset.Add(asset);
    }

    public void EnsureCapacity(int capacity) => Asset.EnsureCapacity(capacity);

    public AssetStoreMeta ToSnapshot() => new(Count, FileCount, kind);


    internal static void Create(IAssetList[] array)
    {
        var kind = AssetEnums.ToAssetKind<T>();
        var idx = (int)kind - 1;

        if (array[idx] != null) throw new ArgumentException(nameof(array));
        array[idx] = new AssetList<T>(kind);
    }
}