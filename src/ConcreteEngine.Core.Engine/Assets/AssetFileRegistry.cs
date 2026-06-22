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
    private readonly Dictionary<AssetFileId, AssetId> _rootBindings = new(DefaultBindingCap);
    
    private readonly Dictionary<string, List<AssetFileId>> _byDirectory = new(32);

    private readonly Stack<int> _free = [];

    internal AssetFileRegistry() { }

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;
    public int Capacity => _files.Length;

    public int RootFileCount => _rootBindings.Count;

    //
    public bool HasFilePath(string relativePath) => _fileByPath.ContainsKey(relativePath);
    public bool IsRootFile(AssetFileId fileId) => _rootBindings.ContainsKey(fileId);

    public bool HasFile(AssetFileId fileId)
    {
        var index = fileId.Index();
        return (uint)index < (uint)_files.Length && _files[index]?.Id == fileId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetFile Get(AssetFileId id)
    {
        if (_files[id.Index()] is { } file && file.Id == id) return file;
        Throwers.NotFoundBy(nameof(AssetFile), id);
        return null;
        //        
    }

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
        if (!_fileByPath.TryGetValue(relativePath, out var fileId))
        {
            entry = null;
            return false;
        }

        return TryGetFile(fileId, out entry);
    }

    public bool TryGetByRootFileId(AssetFileId fileId, out AssetId assetId) =>
        _rootBindings.TryGetValue(fileId, out assetId);

    //

    internal void Replace(AssetFileId id, AssetFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Id, nameof(id));
        _files[id.Index()] = file;
    }

    internal AssetFile RegisterRoot(AssetId assetRootId, string name, in FileScanInfo fileInfo)
    {
        if (!assetRootId.IsValid()) Throwers.InvalidArgument(nameof(assetRootId));
        var fileSpec = AddFile(FileBinding.RootFile, name, in fileInfo);
        _rootBindings.Add(fileSpec.Id, assetRootId);
        return fileSpec;
    }

    internal AssetFile RegisterFile(FileBinding binding, string name, in FileScanInfo scanInfo)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)binding, (int)FileBinding.RootFile, nameof(binding));
        return AddFile(binding, name, in scanInfo);
    }

    private AssetFile AddFile(FileBinding binding, string name, in FileScanInfo scanInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(scanInfo.RelativePath);
        ArgumentOutOfRangeException.ThrowIfEqual(scanInfo.IsValid, false);
        ArgumentOutOfRangeException.ThrowIfZero((int)binding, nameof(binding));

        if (_fileByPath.ContainsKey(scanInfo.RelativePath))
            Throwers.InvalidArgument(nameof(scanInfo), $"AssetFile {scanInfo.RelativePath} already registered");

        var fileId = AllocateSlot();
        var fileSpec = new AssetFile(
            gId: Guid.NewGuid(),
            id: fileId,
            binding: binding,
            storage: scanInfo.Storage,
            logicalName: name,
            relativePath: scanInfo.RelativePath,
            sizeBytes: scanInfo.SizeBytes,
            lastWriteTime: scanInfo.LastWriteTime
        );

        var dirSpan = Path.GetDirectoryName(scanInfo.RelativePath.AsSpan());
        if (!_byDirectory.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(dirSpan, out var fileIds))
            _byDirectory.GetAlternateLookup<ReadOnlySpan<char>>().TryAdd(dirSpan, fileIds = new List<AssetFileId>(9));

        fileIds.Add(fileId);

        _files[fileId.Index()] = fileSpec;
        _fileByPath.Add(scanInfo.RelativePath, fileId);
        return fileSpec;
    }

    private AssetFileId AllocateSlot()
    {
        var freeIndex = SlotHelper.NextSlot(_free, Count);
        if (freeIndex >= 0) return new AssetFileId(freeIndex + 1, 1);

        if (SlotHelper.EnsureCapacity(ref _files, Count, 1, out var oldSize))
            Logger.Log(StringLogEvent.MakeResize(LogScope.Assets, nameof(AssetFileRegistry), oldSize, _files.Length));

        return new AssetFileId(++Count, 1);
    }

    //
    public ActiveObjectEnumerator<AssetFile> GetEnumerator() => new(_files.AsSpan(0, Count));
}