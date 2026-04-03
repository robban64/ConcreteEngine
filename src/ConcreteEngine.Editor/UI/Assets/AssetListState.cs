using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Extensions;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed unsafe class AssetListState(AssetKind pendingKind)
{
    public const string GoBackString = "..";
    public const int MaxFolderCount = 32;
    public const int MaxAssetCount = 128;

    private const int FolderEndAt = String64Utf8.Capacity * MaxFolderCount;

    public static int Capacity => FolderEndAt + (MaxAssetCount * AssetFileDisplayItem.SizeOf);

    public AssetFileId SelectedFileId { get; set; }
    public AssetKind SelectedKind { get; private set; }
    public AssetKind PendingKind { get; private set; } = pendingKind;
    public bool IsRootPath { get; private set; }
    public int FilteredCount { get; private set; }
    public int FileCount { get; private set; }
    public short FolderCount { get; private set; }
    public (short RootEndIndex, short BoundEndIndex) Offset { get; private set; }

    public string? PendingDirectory { get; private set; }

    private readonly byte[] _searchIndices = new byte[128];

    public NativeViewPtr<byte> BreadcrumbStrPtr = NativeViewPtr<byte>.MakeNull();
    public NativeViewPtr<byte> ListBufferPtr = NativeViewPtr<byte>.MakeNull();


    public int TotalDrawCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => FilteredCount + FolderCount;
    }

    public String64Utf8* SubFolderPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (String64Utf8*)ListBufferPtr.Ptr;
    }

    public AssetFileDisplayItem* FileItemPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (AssetFileDisplayItem*)(ListBufferPtr.Ptr + FolderEndAt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeSpan<byte> GetSearchIndices() =>
        new(ref MemoryMarshal.GetArrayDataReference(_searchIndices), FilteredCount);


    public void EnqueueNewAssetKind(AssetKind kind)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero((int)kind, nameof(kind));
        if (PendingKind != AssetKind.Unknown)
            throw new InvalidOperationException($"Already pending. Pending kind: {PendingKind.ToText()}");
        if (PendingDirectory != null)
            throw new InvalidOperationException($"Already pending. Pending directory: {PendingDirectory}");

        PendingKind = kind;
    }

    public void SetBuffer(NativeViewPtr<byte> buffer)
    {
        /*
        ArgumentOutOfRangeException.ThrowIfEqual(buffer.IsNull, true);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, Capacity);
        _buffer = buffer;
        _subFolders = (String64Utf8*)_buffer.Ptr;
        _entries = (AssetFileDisplayItem*)(_buffer.Ptr + AssetObject.MaxNameLength * 32);
        */
    }

    public void EnqueueDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        if (PendingKind != AssetKind.Unknown)
            throw new InvalidOperationException($"Already pending. Pending kind: {PendingKind.ToText()}");
        if (PendingDirectory != null)
            throw new InvalidOperationException($"Already pending. Pending directory: {PendingDirectory}");

        PendingDirectory = directory;
    }

    public void UpdateTitleText(AssetBrowser assetBrowser)
    {
        var dirSpan = assetBrowser.CurrentDirectory.AsSpan();
        var sw = BreadcrumbStrPtr.Writer();
        sw.Append('[').Append(FilteredCount).Append(']').PadRight(2).Append('/');
        foreach (var range in dirSpan.Split('/'))
            sw.Append(dirSpan[range]).Append('/');

        // remove last '/'
        sw.SetCursor(sw.Cursor - 1);
        sw.Append((char)0);
    }


    public bool SyncStateToBrowser(AssetBrowser assetBrowser)
    {
        if (PendingKind == AssetKind.Unknown && PendingDirectory == null) return false;

        if (PendingKind != AssetKind.Unknown && PendingKind != SelectedKind)
        {
            SelectedKind = PendingKind;
            PendingKind = AssetKind.Unknown;
            PendingDirectory = SelectedKind.ToRootFolder();
        }

        SetAssetDirectory(assetBrowser);
        return true;
    }

    private void SetAssetDirectory(AssetBrowser assetBrowser)
    {
        if (SelectedKind == AssetKind.Unknown || PendingDirectory == null) return;

        IsRootPath = SelectedKind.ToRootFolder() == PendingDirectory;
        var hasPath = !IsRootPath && PendingDirectory.IndexOf('/') > 0;

        if (IsRootPath || hasPath)
            assetBrowser.SetDirectory(PendingDirectory, SelectedKind);
        else if (PendingDirectory == GoBackString)
            assetBrowser.SetToParentDirectory();
        else
            assetBrowser.SetLocalDirectory(PendingDirectory);

        UpdateFolderAndEntries(assetBrowser, EngineObjectStore.AssetProvider);
        UpdateTitleText(assetBrowser);
        PendingDirectory = null;
    }

    private void UpdateFolderAndEntries(AssetBrowser assetBrowser, AssetProvider provider)
    {
        var prevLen = FolderCount * String64Utf8.Capacity +
                      FileCount * AssetFileDisplayItem.SizeOf;

        if (prevLen > 0)
            ListBufferPtr.AsSpan(0, prevLen).Clear();

        var currentNode = assetBrowser.CurrentNode;
        int folderCount = currentNode.FolderCount, fileCount = currentNode.FileCount;
        if (folderCount > MaxFolderCount || fileCount > MaxAssetCount)
            throw new InvalidOperationException("Overflow, fix size management");

        var ptrIdx = 0;
        for (var i = 0; i < folderCount; i++)
        {
            var ptr = (String64Utf8*)ListBufferPtr.Ptr;
            ptr[i] = new String64Utf8(currentNode.Children[i].FolderName);
        }

        var filePtr = FileItemPtr;
        for (var i = 0; i < fileCount; i++)
        {
            var fileId = currentNode.FileIds[i];
            var file = provider.GetFileSpec(fileId);
            var assetId = provider.TryGetByRootFile(fileId, out var asset) ? asset.Id : AssetId.Empty;
            if (provider.IsUnboundFile(fileId)) assetId = new AssetId(-1);
            filePtr[i] = new AssetFileDisplayItem(fileId, assetId, file.LogicalName);
        }

        FolderCount = (short)folderCount;
        FileCount = fileCount;
        SetSearch(0, 0);
    }

    public void SetSearch(ulong searchKey, ulong searchMask)
    {
        Offset = (-1, -1);

        var fileCount = FileCount;
        var filePtr = FileItemPtr;
        var searchIndices = _searchIndices.AsSpan();
        searchIndices.Clear();

        short count = 0;
        if (searchKey == 0)
        {
            count = (short)fileCount;
            for (byte i = 0; i < fileCount; i++)
            {
                searchIndices[i] = i;
                var assetId = filePtr[i].AssetRootId;
                if (Offset.RootEndIndex == -1 && assetId == 0) Offset = (i, Offset.BoundEndIndex);
                else if (Offset.BoundEndIndex == -1 && assetId == -1) Offset = (Offset.RootEndIndex, i);
            }
        }
        else
        {
            for (byte i = 0; i < fileCount; i++)
            {
                var packedName = FileItemPtr[i].PackedName;
                if ((packedName & searchMask) != searchKey) continue;
                searchIndices[count++] = i;
                
                var assetId = filePtr[i].AssetRootId;
                if (Offset.RootEndIndex == -1 && assetId == 0) Offset = (count, Offset.BoundEndIndex);
                else if (Offset.BoundEndIndex == -1 && assetId == -1) Offset = (Offset.RootEndIndex, count);
            }

        }
        if(Offset.RootEndIndex == -1) Offset = (count, -1);
        FilteredCount = count;
    }
}