using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Configuration;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class AssetFileRegistry
{
    private const int DefaultCap = 512;

    public int Count { get; private set; }

    private AssetFile?[] _files = new AssetFile?[DefaultCap];

    private readonly Dictionary<string, AssetFileId> _fileByPath = new(DefaultCap);
    private readonly Dictionary<string, List<AssetFileId>> _byDirectory = new(32);

    private readonly Stack<int> _free = [];

    internal AssetFileRegistry() { }

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;
    public int Capacity => _files.Length;

    internal ReadOnlySpan<AssetFile?> GetFileSpan() => _files.AsSpan(0, Count);

    public Dictionary<string, List<AssetFileId>>.KeyCollection GetDirectories() => _byDirectory.Keys;

    //
    public bool HasFilePath(string relativePath) => _fileByPath.ContainsKey(relativePath);

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

    public ReadOnlySpan<AssetFileId> GetDirectoryIds(string path)
    {
        return _byDirectory.TryGetValue(path, out var fileIdList) ? CollectionsMarshal.AsSpan(fileIdList) : default;
    }

    public bool TryGetDirectoryIds(string path, out ReadOnlySpan<AssetFileId> fileIds)
    {
        if (!_byDirectory.TryGetValue(path, out var fileIdList))
        {
            fileIds = ReadOnlySpan<AssetFileId>.Empty;
            return false;
        }
        
        fileIds = CollectionsMarshal.AsSpan(fileIdList);
        return true;
    }


    //

    internal void Replace(AssetFileId id, AssetFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Id, nameof(id));
        _files[id.Index()] = file;
    }

    internal AssetFile RegisterRoot(AssetId assetRootId, string assetName, Guid assetGuid,
        in FileScanInfo scanInfo)
    {
        if (!assetRootId.IsValid()) Throwers.InvalidArgument(nameof(assetRootId));
        var fileId = AllocateSlot();
        var file = AssetFile.MakeRoot(fileId, assetRootId, assetName, assetGuid, in scanInfo);
        AddFile(file);
        return file;
    }

    internal AssetFile RegisterFile(bool isUnbound, in FileScanInfo scanInfo)
    {
        var fileId = AllocateSlot();
        var file = AssetFile.MakeFile(fileId, isUnbound, in scanInfo);
        AddFile(file);
        return file;
    }


    private void AddFile(AssetFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Storage == AssetStorage.InMemory)
        {
            _files[file.Id.Index()] = file;
            return;
        }

        if (!file.RelativePath.StartsWith(EnginePath.AssetBasePath))
            Throwers.InvalidArgument(nameof(file.RelativePath));

        if (_fileByPath.ContainsKey(file.RelativePath))
            Throwers.InvalidArgument(nameof(file), $"AssetFile {file.RelativePath} already registered");

        var path = Path.GetDirectoryName(file.RelativePath.AsSpan());
        if (!_byDirectory.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(path, out var fileIds))
            _byDirectory.Add(path.ToString(), fileIds = new List<AssetFileId>(4));

        fileIds.Add(file.Id);

        _fileByPath.Add(file.RelativePath, file.Id);
        _files[file.Id.Index()] = file;
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

    public SparseObjectEnumerator<AssetFileId, AssetFile> MakeSparseEnumerator(ReadOnlySpan<AssetFileId> fileIds) =>
        new(fileIds, _files.AsSpan(0, Count));
}