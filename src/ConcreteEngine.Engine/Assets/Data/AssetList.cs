using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets.Data;

public abstract class AssetList
{
    public abstract int Count { get; }
    public abstract int FileCount { get; internal set; }
    public abstract AssetKind Kind { get; }
    public abstract ReadOnlySpan<AssetObject> GetAssetObjects();
    public abstract AssetsMetaInfo ToSnapshot();
}

public sealed class AssetList<T>(AssetKind kind) : AssetList where T : AssetObject
{
    internal List<T> Asset { get; } = [];

    public override int FileCount { get; internal set; }
    public override int Count => Asset.Count;
    public override AssetKind Kind => kind;

    public ReadOnlySpan<T> GetAssets() => CollectionsMarshal.AsSpan(Asset);
    public override ReadOnlySpan<AssetObject> GetAssetObjects() => CollectionsMarshal.AsSpan(Asset);

    public void Add(T asset, int fileSpecs)
    {
        FileCount += fileSpecs;
        Asset.Add(asset);
    }

    public override AssetsMetaInfo ToSnapshot() => new(Count, FileCount, kind);


    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureCapacity(int capacity) => Asset.EnsureCapacity(capacity);

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Create(AssetList[] array)
    {
        var kind = AssetKindUtils.ToAssetKind<T>();
        var idx = (int)kind - 1;

        if (array[idx] != null) throw new ArgumentException(nameof(array));
        array[idx] = new AssetList<T>(kind);
    }
}