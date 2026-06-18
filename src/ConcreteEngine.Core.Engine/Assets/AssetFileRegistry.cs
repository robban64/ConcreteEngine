using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets.Descriptors;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class AssetFileRegistry
{
    private const int DefaultCap = 512;
    private const int DefaultBindingCap = 256;

    public int Count { get; private set; }

    private AssetFile?[] _files = new AssetFile?[DefaultCap];
    
    private readonly Dictionary<string, AssetFileId> _fileByPath = new(DefaultCap);

    private readonly Dictionary<AssetId, AssetFileId[]> _fileBindings = new(DefaultBindingCap);
    private readonly Dictionary<AssetFileId, AssetId> _rootBindings = new(DefaultBindingCap);


    private readonly List<AssetFileId> _rootFiles = new(DefaultBindingCap);
    private readonly List<AssetFileId> _dependentFiles = new(DefaultBindingCap);
    private readonly List<AssetFileId> _unboundFiles = new(DefaultBindingCap);

    private readonly Stack<int> _free = [];

    internal AssetFileRegistry() { }

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;
    public int Capacity => _files.Length;

    public int RootFileCount => _rootBindings.Count;
    public int DependentFileCount => _dependentFiles.Count;
    public int UnboundFileCount => _unboundFiles.Count;
    
    public bool HasFilePath(string relativePath) => _fileByPath.ContainsKey(relativePath);
    public bool HasBinding(AssetId assetId) => _fileBindings.ContainsKey(assetId);
    public bool HasFile(AssetFileId fileId)
    {
        var index = fileId.Index();
        return (uint)index < (uint)_files.Length && _files[index]?.Id == fileId;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileId> GetRootFileIdSpan() => CollectionsMarshal.AsSpan(_rootFiles);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileId> GetDependentFileIdSpan() => CollectionsMarshal.AsSpan(_dependentFiles);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileId> GetUnboundFileIdSpan() => CollectionsMarshal.AsSpan(_unboundFiles);


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
        var index = id.Index();
        if ((uint)index >= (uint)_files.Length || _files[index] is not { } file || file.Id != id)
        {
            entry = null;
            return false;
        }

        entry = file;
        return true;
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

   // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FileBinding GetFileBindingStatus(AssetFileId fileId)
    {
        if (_rootBindings.ContainsKey(fileId)) return FileBinding.RootFile;
        return _dependentFiles.BinarySearchUnmanaged(fileId) >= 0 ? FileBinding.DependentFile : FileBinding.UnboundFile;
    }

    //
    private AssetFile AddFile(string name, string relativePath, int fileCount, in FileScanInfo fileInfo)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fileCount);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(relativePath);

        if (_fileByPath.ContainsKey(relativePath))
            throw new InvalidOperationException($"AssetFile {relativePath} already registered");

        var fileId = AllocateSlot();
        var fileSpec = MakeFileSpec(fileId, name, relativePath, in fileInfo);

        _files[fileId.Index()] = fileSpec;
        _fileByPath.Add(relativePath, fileId);

        return fileSpec;
    }

    internal AssetFile Register(AssetId assetRootId, string name, string relativePath, int fileCount,
        in FileScanInfo fileInfo)
    {
        var fileSpec = AddFile(name, relativePath, fileCount, in fileInfo);

        if (!assetRootId.IsValid())
        {
            _dependentFiles.Add(fileSpec.Id);
            return fileSpec;
        }

        var fileBindings = new AssetFileId[fileCount + 1];
        fileBindings[0] = fileSpec.Id;
        _fileBindings.Add(assetRootId, fileBindings);
        _rootBindings.Add(fileSpec.Id, assetRootId);
        _rootFiles.Add(fileSpec.Id);

        return fileSpec;
    }

    internal AssetFile RegisterUnbound(string name, string relativePath, in FileScanInfo fileInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(relativePath);
        if (_fileByPath.ContainsKey(relativePath))
            throw new InvalidOperationException($"Unbound File '{relativePath}' already registered");

        var file = AddFile(name, relativePath, 0, in fileInfo);
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

    private AssetFileId AllocateSlot()
    {
        var freeIndex = SlotHelper.NextSlot(_free, Count);
        if (freeIndex >= 0) return new AssetFileId(freeIndex + 1);

        if (SlotHelper.EnsureCapacity(ref _files, Count, 1, out var oldSize))
            Logger.Log(StringLogEvent.MakeResize(LogScope.Assets, nameof(AssetFileRegistry), oldSize, _files.Length));

        return new AssetFileId(++Count);
    }
    //
    public ActiveObjectEnumerator<AssetFile> GetEnumerator() => new(_files.AsSpan(0, Count));
    public AssetBindingEnumerator AssetBindingsEnumerator(AssetId assetId) => new(assetId, this);

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
            Storage: scanInfo.Storage,
            SizeBytes: scanInfo.SizeBytes,
            LastWriteTime: scanInfo.LastWriteTime,
            ContentHash: null,
            Source: scanInfo.Source
        );
    }
}