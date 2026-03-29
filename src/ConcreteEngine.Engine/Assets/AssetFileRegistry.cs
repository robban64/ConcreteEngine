using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;

namespace ConcreteEngine.Engine.Assets;

internal sealed class AssetFileRegistry
{
    private const int DefaultCap = 512;

    private AssetFileId MakeAssetFileId() => new(_files.AllocateNext() + 1);

    private readonly SlotArray<AssetFileSpec> _files = new(DefaultCap);

    private readonly Dictionary<AssetId, AssetFileId[]> _fileBindings = new(DefaultCap);
    private readonly Dictionary<AssetFileId, AssetId> _rootBindings = new(DefaultCap);
    private readonly Dictionary<string, int> _fileByPath = new(DefaultCap);
    private readonly List<int> _unboundFiles = new(64);

    public ReadOnlySpan<AssetFileId> GetUnboundFileIds()
        => MemoryMarshal.Cast<int, AssetFileId>(CollectionsMarshal.AsSpan(_unboundFiles));

    public bool IsUnboundFile(AssetFileId fileId) => _unboundFiles.Contains(fileId);

    public bool HasFilePath(string relativePath) => _fileByPath.ContainsKey(relativePath);
    public bool HasBinding(AssetId assetId) => _fileBindings.ContainsKey(assetId);

    public bool HasFile(AssetFileId fileId)
    {
        var index = fileId.Index();
        return (uint)index < (uint)_files.Capacity && _files[index]?.Id == fileId;
    }

    public AssetFileSpec Add(AssetId assetRootId, string name, string relativePath, int fileCount,
        in FileScanInfo fileInfo)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fileCount);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(relativePath);

        if (_fileByPath.ContainsKey(relativePath))
            throw new InvalidOperationException($"AssetFile {relativePath} already registered");

        var fileId = MakeAssetFileId();
        var fileSpec = MakeFileSpec(fileId, name, relativePath, in fileInfo);

        _files[fileId.Index()] = fileSpec;
        _fileByPath.Add(relativePath, fileId);

        if (assetRootId.IsValid())
        {
            var fileBindings = new AssetFileId[fileCount + 1];
            fileBindings[0] = fileId;
            _fileBindings.Add(assetRootId, fileBindings);
            _rootBindings.Add(fileId, assetRootId);
        }

        return fileSpec;
    }

    public AssetFileSpec AddUnbound(string name, string relativePath, in FileScanInfo fileInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(relativePath);
        if (_fileByPath.ContainsKey(relativePath))
            throw new InvalidOperationException($"Unbound File '{relativePath}' already registered");

        var file = Add(AssetId.Empty, name, relativePath, 0, in fileInfo);
        _unboundFiles.Add(file.Id);
        return file;
    }

    public AssetFileSpec Get(AssetFileId id) => _files[id.Index()];

    public void Replace(AssetFileId id, AssetFileSpec fileSpec)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Value, nameof(id));
        ArgumentNullException.ThrowIfNull(fileSpec);
        _files[id.Index()] = fileSpec;
    }

    public bool TryGetFile(AssetFileId id, out AssetFileSpec entry)
    {
        entry = null!;
        var index = id.Index();
        if ((uint)index > (uint)_files.Capacity) return false;
        return (entry = _files[index]) != null;
    }

    public bool TryGetFileByPath(string relativePath, out AssetFileSpec entry)
    {
        entry = null!;
        return _fileByPath.TryGetValue(relativePath, out var fileId) && TryGetFile(new AssetFileId(fileId), out entry);
    }

    internal bool TryGetByRootFileId(AssetFileId fileId, out AssetId assetId) =>
        _rootBindings.TryGetValue(fileId, out assetId);

    public Span<AssetFileId> GetAssetFileBindings(AssetId id)
    {
        return _fileBindings[id];
    }

    public bool TryGetFileBindings(AssetId id, out Span<AssetFileId> bindings)
    {
        bindings = Span<AssetFileId>.Empty;
        if (_fileBindings.TryGetValue(id, out var res)) bindings = res;
        return !bindings.IsEmpty;
    }


    private static AssetFileSpec MakeFileSpec(AssetFileId id, string name, string path, in FileScanInfo scanInfo)
    {
        return new AssetFileSpec(
            Id: id,
            GId: Guid.NewGuid(),
            LogicalName: name,
            RelativePath: path,
            Storage: scanInfo.StorageKind,
            SizeBytes: scanInfo.SizeBytes,
            LastWriteTime: scanInfo.LastWriteTime,
            ContentHash: null,
            Source: scanInfo.Source
        );
    }

    public AssetFilesEnumerator GetAssetFilesEnumerator(AssetId assetId) => new(assetId, this);

    public ref struct AssetFilesEnumerator(AssetId assetId, AssetFileRegistry registry)
    {
        private int _i = -1;
        private readonly Span<AssetFileId> _fileIds = registry.GetAssetFileBindings(assetId);

        public bool MoveNext() => ++_i < _fileIds.Length;
        public readonly AssetFileSpec Current => registry.Get(_fileIds[_i]);
    }
}