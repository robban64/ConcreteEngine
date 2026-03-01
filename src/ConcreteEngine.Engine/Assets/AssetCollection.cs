using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets;

public abstract class AssetCollection
{
    internal readonly HashSet<AssetId> DirtyIds = new(32);

    public abstract int Count { get; }
    public abstract int FileCount { get; internal set; }
    public abstract AssetKind Kind { get; }
    public abstract ReadOnlySpan<AssetObject> GetAssetObjectSpan();
    public abstract AssetsMetaInfo ToSnapshot();

    internal void MarkDirty(AssetId id) => DirtyIds.Add(id);
    internal void ClearDirty() => DirtyIds.Clear();
}

public sealed class AssetCollection<T>(AssetKind kind) : AssetCollection where T : AssetObject
{
    private readonly List<T> _asset = [];

    public override int FileCount { get; internal set; }
    
    public override int Count => _asset.Count;
    public override AssetKind Kind => kind;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> GetAssetSpan() => CollectionsMarshal.AsSpan(_asset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ReadOnlySpan<AssetObject> GetAssetObjectSpan() => CollectionsMarshal.AsSpan(_asset);

    public void Add(T asset, int fileSpecs)
    {
        FileCount += fileSpecs;
        _asset.Add(asset);
    }

    public override AssetsMetaInfo ToSnapshot() => new(Count, FileCount, kind);


    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureCapacity(int capacity) => _asset.EnsureCapacity(capacity);

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Create(AssetCollection[] array)
    {
        var kind = AssetKindUtils.ToAssetKind<T>();
        var idx = (int)kind - 1;

        if (array[idx] != null) throw new ArgumentException(nameof(array));
        array[idx] = new AssetCollection<T>(kind);
    }
}