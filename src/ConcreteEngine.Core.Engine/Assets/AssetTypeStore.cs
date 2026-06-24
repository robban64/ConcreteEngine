using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class AssetTypeStore(AssetKind kind)
{
    private readonly List<AssetId> _asset = [];
    private readonly List<int> _dirtyIds = [];
    private readonly Dictionary<string, AssetId> _byName = [];

    public readonly AssetKind Kind = kind;

    public int Count => _asset.Count;
    public int DirtyCount => _dirtyIds.Count;

    public AssetsMetaInfo ToSnapshot() => new(Count, 0, Kind);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<int> GetDirtySpan() => CollectionsMarshal.AsSpan(_dirtyIds);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetId> AsSpan() => CollectionsMarshal.AsSpan(_asset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasName(string name) => _byName.ContainsKey(name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetByName(string name, out AssetId assetId) => _byName.TryGetValue(name, out assetId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetId GetByName(string name)
    {
        if (TryGetByName(name, out var value)) return value;
        Throwers.NotFoundBy(nameof(name), name);
        return AssetId.Empty;
    }

    internal void Add(AssetObject asset)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)asset.Kind, (int)Kind, nameof(asset));
        _asset.Add(asset.Id);
        _byName.Add(asset.Name, asset.Id);
    }

    internal void Rename(string oldName, string newName)
    {
        if (_byName.ContainsKey(newName))
            throw new ArgumentException("Rename: name already exists", nameof(newName));

        var id = GetByName(oldName);
        _byName.Remove(oldName);
        _byName.Add(newName, id);
    }

    internal void MarkDirty(AssetObject asset)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(asset.Id.Id, nameof(asset.Id));
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)asset.Kind, (int)Kind, nameof(asset));

        var id = asset.Id.Id;
        if (_dirtyIds.Count == 0)
        {
            _dirtyIds.Add(id);
            return;
        }

        var lastId = _dirtyIds[^1];
        if (lastId == id) return;

        if (id > lastId)
        {
            _dirtyIds.Add(id);
            return;
        }

        var existingIndex = SearchMethod.BinarySearch(CollectionsMarshal.AsSpan(_dirtyIds), id);
        if (existingIndex >= 0) return;
        _dirtyIds.Add(id);
        _dirtyIds.Sort();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ClearDirty() => _dirtyIds.Clear();

    internal void Sort() => _asset.Sort();

    internal void EnsureCapacity(int capacity)
    {
        _asset.EnsureCapacity(capacity);
        _dirtyIds.EnsureCapacity(capacity);
        _byName.EnsureCapacity(capacity);
    }
}