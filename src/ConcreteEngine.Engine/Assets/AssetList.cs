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

internal sealed class AssetList<T>(AssetKind kind, int capacity) : IAssetList where T : AssetObject
{
    private readonly List<T> _assets = new(capacity);

    public int FileCount { get; internal set; }
    public int Count => _assets.Count;
    public AssetKind Kind => kind;

    internal ReadOnlySpan<T> AssetSpan => CollectionsMarshal.AsSpan(_assets);
    internal List<T> Asset => _assets;

    public void Add(T asset, int fileSpecs)
    {
        FileCount += fileSpecs;
        _assets.Add(asset);
    }

    public void EnsureCapacity(int capacity) => _assets.EnsureCapacity(capacity);

    public AssetStoreMeta ToSnapshot() => new(Count, FileCount, kind);


    internal static void Create(IAssetList[] array, int cap)
    {
        var kind = AssetEnums.ToAssetKind<T>();
        var idx = (int)kind - 1;

        if (array[idx] != null) throw new ArgumentException(nameof(array));
        array[idx] = new AssetList<T>(kind, cap);
    }
}