using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;

namespace ConcreteEngine.Engine.Assets;

internal sealed class AssetFileRegistry
{
    private const int DefaultCap = 512;

    private AssetFileId MakeAssetFileId() => new(++_assetFileId);

    private int _assetFileId;

    private readonly Dictionary<int, AssetFileSpec> _files = new(DefaultCap);
    private readonly Dictionary<string, int> _fileByName = new(DefaultCap);

    private readonly Dictionary<AssetId, AssetFileId[]> _fileBindings = new(DefaultCap);
    private readonly HashSet<int> _pendingFiles = new(64);

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPendingFile(AssetFileId id) => _pendingFiles.Contains(id);

    public ReadOnlySpan<AssetFileId> GetFileIds(AssetId assetId)
    {
        if (TryGetFileIds(assetId, out var fileIds)) return fileIds;
        throw new InvalidCastException($"Asset TryGetFileIds '{assetId}' not found or incorrect type.");
    }

    public bool TryGetFileEntry(AssetFileId id, out AssetFileSpec entry) => _files.TryGetValue(id, out entry!);

    public bool TryGetFileIds(AssetId id, out ReadOnlySpan<AssetFileId> fileIds)
    {
        fileIds = ReadOnlySpan<AssetFileId>.Empty;
        if (_fileBindings.TryGetValue(id, out var res)) fileIds = res;
        return !fileIds.IsEmpty;
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