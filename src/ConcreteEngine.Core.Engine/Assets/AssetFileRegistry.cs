using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Engine.Assets.Descriptors;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class AssetFileRegistry
{
    private const int DefaultCap = 512;

    private AssetFileId MakeAssetFileId() => new(_files.AllocateNext() + 1);

    private readonly SlotArray<AssetFile> _files = new(DefaultCap);
    private readonly Dictionary<AssetId, AssetFileId[]> _fileBindings = new(DefaultCap);
    private readonly Dictionary<AssetFileId, AssetId> _rootBindings = new(DefaultCap);

    private readonly Dictionary<string, AssetFileId> _fileByPath = new(DefaultCap); // string, AssetFileId

    private readonly List<AssetFileId> _dependentFiles = new(64);
    private readonly List<AssetFileId> _unboundFiles = new(64);

    internal AssetFileRegistry()
    {
        _files.OnResize = static (oldSize, newSize) =>
            Logger.Log(StringLogEvent.MakeResize(LogScope.Assets, nameof(AssetFileRegistry), oldSize, newSize));
    }

    public int Count => _files.Count;
    public int Capacity => _files.Capacity;

    public bool HasFilePath(string relativePath) => _fileByPath.ContainsKey(relativePath);
    public bool HasBinding(AssetId assetId) => _fileBindings.ContainsKey(assetId);

    public bool HasFile(AssetFileId fileId)
    {
        var index = fileId.Index();
        return (uint)index < (uint)_files.Capacity && _files[index]?.Id == fileId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FileBinding GetFileBindingStatus(AssetFileId fileId)
    {
        if (_rootBindings.ContainsKey(fileId)) return FileBinding.RootFile;
        var span = MemoryMarshal.Cast<AssetFileId, int>(CollectionsMarshal.AsSpan(_dependentFiles));
        return span.IndexOf(fileId.Value) >= 0 ? FileBinding.DependentFile : FileBinding.UnboundFile;
    }

    internal AssetFile Add(AssetId assetRootId, string name, string relativePath, int fileCount,
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
        else
        {
            _dependentFiles.Add(fileId);
        }

        return fileSpec;
    }

    internal AssetFile AddUnbound(string name, string relativePath, in FileScanInfo fileInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(relativePath);
        if (_fileByPath.ContainsKey(relativePath))
            throw new InvalidOperationException($"Unbound File '{relativePath}' already registered");

        var file = Add(AssetId.Empty, name, relativePath, 0, in fileInfo);
        _unboundFiles.Add(file.Id);
        return file;
    }

    internal void Replace(AssetFileId id, AssetFile file)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Value, nameof(id));
        ArgumentNullException.ThrowIfNull(file);
        _files[id.Index()] = file;
    }

    internal AssetFile UpdateFileSpec(AssetFileId fileId, in FileScanInfo fileInfo)
    {
        if (!TryGetFile(fileId, out var file))
            throw new ArgumentException($"File {fileId} does not exist", nameof(fileId));

        return _files[fileId.Index()] = MakeFileSpecCopy(file, in fileInfo);
    }

    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetFile Get(AssetFileId id)
    {
        var it = _files[id.Index()];
        if (it is null) Throwers.InvalidHandle(id);
        return it;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetFile GetAssetRootFile(AssetId id) => Get(_fileBindings[id][0]);

    public bool TryGetFile(AssetFileId id, [NotNullWhen(true)] out AssetFile? entry)
    {
        return _files.TryGet(id.Index(), out entry) && entry.Id == id;
    }

    public bool TryGetFileByPath(string relativePath, [NotNullWhen(true)] out AssetFile? entry)
    {
        entry = null!;
        return _fileByPath.TryGetValue(relativePath, out var fileId) && TryGetFile(fileId, out entry);
    }

    public bool TryGetByRootFileId(AssetFileId fileId, out AssetId assetId)
    {
        var res = _rootBindings.TryGetValue(fileId, out var handle);
        assetId = res ? handle : default;
        return res;
    }

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileId> GetUnboundFileIds() => CollectionsMarshal.AsSpan(_unboundFiles);

    public Span<AssetFileId> GetFileBindings(AssetId id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Value, nameof(id));
        return _fileBindings[id];
    }

    public bool TryGetFileBindings(AssetId id, out Span<AssetFileId> bindings)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Value, nameof(id));
        bindings = Span<AssetFileId>.Empty;
        if (_fileBindings.TryGetValue(id, out var res)) bindings = res;
        return !bindings.IsEmpty;
    }

    public AssetFilesEnumerator AssetBindingsEnumerator(AssetId assetId) => new(assetId, this);

    private static AssetFile MakeFileSpecCopy(AssetFile file, in FileScanInfo scanInfo)
    {
        return file with
        {
            SizeBytes = scanInfo.SizeBytes,
            LastWriteTime = scanInfo.LastWriteTime,
            ContentHash = null,
            Source = scanInfo.Source
        };
    }

    private static AssetFile MakeFileSpec(AssetFileId id, string name, string path, in FileScanInfo scanInfo)
    {
        return new AssetFile(
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
}