using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets;

public abstract class AssetCollection
{
    protected readonly List<AssetFileSpec> Files = [];

    internal readonly HashSet<AssetId> DirtyIds = new(32);

    public abstract int Count { get; }
    public abstract AssetKind Kind { get; }
    public int FileCount => Files.Count;
    public abstract ReadOnlySpan<AssetObject> GetAssetObjectSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileSpec> GetFileSpan() => CollectionsMarshal.AsSpan(Files);

    public AssetsMetaInfo ToSnapshot() => new(Count, FileCount, Kind);

    public void AddFile(AssetFileSpec fileSpec) => Files.Add(fileSpec);
    
    internal abstract void Sort();
    internal void MarkDirty(AssetId id) => DirtyIds.Add(id);
    internal void ClearDirty() => DirtyIds.Clear();
}

public sealed class AssetCollection<T>(AssetKind kind) : AssetCollection where T : AssetObject
{
    private readonly List<T> _asset = [];

    public override int Count => _asset.Count;
    public override AssetKind Kind => kind;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> GetAssetSpan() => CollectionsMarshal.AsSpan(_asset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ReadOnlySpan<AssetObject> GetAssetObjectSpan() => CollectionsMarshal.AsSpan(_asset);

    public void Add(T asset) => _asset.Add(asset);
    
    internal override void Sort()
    {
        _asset.Sort();
        Files.Sort();
    }

    public void EnsureCapacity(int capacity)
    {
        _asset.EnsureCapacity(capacity);
        Files.EnsureCapacity(capacity);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Create(AssetCollection[] array)
    {
        var kind = AssetKindUtils.ToAssetKind(typeof(T));
        var idx = (int)kind - 1;

        if (array[idx] != null) throw new ArgumentException(nameof(array));
        array[idx] = new AssetCollection<T>(kind);
    }
}