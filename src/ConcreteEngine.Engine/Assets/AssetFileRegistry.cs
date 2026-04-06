using System.Runtime.CompilerServices;
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
   
    private readonly Dictionary<string, int> _fileByPath = new(DefaultCap); // string, AssetFileId
    
    private readonly List<AssetFileId> _dependentFiles = new(64);
    private readonly List<AssetFileId> _unboundFiles = new(64);


    public bool HasFilePath(string relativePath) => _fileByPath.ContainsKey(relativePath);
    public bool HasBinding(AssetId assetId) => _fileBindings.ContainsKey(assetId);

    public bool HasFile(AssetFileId fileId)
    {
        var index = fileId.Index();
        return (uint)index < (uint)_files.Capacity && _files[index]?.Id == fileId;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FileSpecBinding GetFileBindingStatus(AssetFileId fileId)
    {
        if (_rootBindings.ContainsKey(fileId)) return FileSpecBinding.RootFile;
        var span = MemoryMarshal.Cast<AssetFileId, int>(CollectionsMarshal.AsSpan(_dependentFiles));
        return span.Contains(fileId) ? FileSpecBinding.DependentFile : FileSpecBinding.UnboundFile;
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
        else
        {
            _dependentFiles.Add(fileId);
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

    //
    internal ReadOnlySpan<AssetFileSpec> GetAllFileSpecs() => _files.AsSpan();

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

}