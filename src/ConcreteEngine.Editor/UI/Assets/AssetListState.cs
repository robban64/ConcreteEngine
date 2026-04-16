using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Extensions;

namespace ConcreteEngine.Editor.UI.Assets;

internal readonly struct FileDisplayItem(AssetFileId fileId, RangeU16 nameHandle, FileSpecBinding binding)
{
    public readonly AssetFileId FileId = fileId;
    public readonly RangeU16 NameHandle = nameHandle;
    public readonly FileSpecBinding Binding = binding;
}

internal sealed unsafe class AssetListState(AssetBrowser assetBrowser, AssetKind pendingKind)
{
    public const string GoBackString = "..";
    public const int MaxItems = 128;
    private const int NameLength = 64;

    public const int NameListCapacity = MaxItems * NameLength;

    //
    public AssetFileId SelectedFileId { get; set; }
    public AssetKind PendingKind { get; private set; } = pendingKind;
    public bool PendingFilter { get; private set; }
    public string? PendingDirectory { get; private set; }
    public int FilteredCount { get; private set; }

    private readonly byte[] _searchIndices = new byte[MaxItems];
    private readonly FileDisplayItem[] _displayItems = new FileDisplayItem[MaxItems];

    public ArenaBlockPtr Memory;
    public Range32 NameListHandle;

    public NativeView<byte> NameList
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Memory.DataPtr.Slice(NameListHandle);
    }

    //
    private AssetKind CurrentKind => assetBrowser.CurrentKind;

    public NativeView<byte> GetName(int i)
    {
        var handle = _displayItems[i].NameHandle;
        return NameList.Slice(handle.Offset, handle.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<byte> GetDrawData(byte i, out FileDisplayItem it)
    {
        it = _displayItems[i];
        return NameList.Slice(it.NameHandle);
    }

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

    public void EnqueueDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        if (PendingKind != AssetKind.Unknown)
            throw new InvalidOperationException($"Already pending. Pending kind: {PendingKind.ToText()}");
        if (PendingDirectory != null)
            throw new InvalidOperationException($"Already pending. Pending directory: {PendingDirectory}");

        PendingDirectory = directory;
    }


    public bool Sync(AssetId renamedAsset)
    {
        if (renamedAsset.IsValid())
        {
            UpdateFolderAndEntries(EngineObjectStore.AssetProvider);
            return true;
        }

        if (PendingFilter) return !(PendingFilter = false);
        if (PendingKind == AssetKind.Unknown && PendingDirectory == null) return false;

        if (PendingKind != AssetKind.Unknown && PendingKind != CurrentKind)
            PendingDirectory = PendingKind.ToRootFolder();

        SetAssetDirectory();
        PendingKind = AssetKind.Unknown;
        return true;
    }

    private void SetAssetDirectory()
    {
        var kind = PendingKind != AssetKind.Unknown ? PendingKind : CurrentKind;
        var directory = PendingDirectory ?? kind.ToRootFolder();
        if (kind == AssetKind.Unknown) return;

        var isRootPath = kind.ToRootFolder() == directory;
        var hasPath = !isRootPath && directory.IndexOf('/') > 0;

        if (isRootPath || hasPath)
            assetBrowser.SetDirectory(directory, kind);
        else if (directory == GoBackString)
            assetBrowser.SetToParentDirectory();
        else
            assetBrowser.SetLocalDirectory(directory);

        UpdateFolderAndEntries(EngineObjectStore.AssetProvider);
        PendingDirectory = null;
    }


    private void UpdateRename(AssetId assetId)
    {
        var currentNode = assetBrowser.CurrentNode;
        var fileCount = currentNode.FileCount;
        var folderCount = currentNode.FolderCount;

        var file = EngineObjectStore.AssetProvider.GetAssetRootFile(assetId);
        var fileId = file.Id;
        for (var i = 0; i < fileCount; i++)
        {
            var it = _displayItems[i];
            if (it.FileId == fileId)
            {
                GetName(i + folderCount).Writer().Write(file.LogicalName);
                return;
            }
        }

        Console.WriteLine("AssetList Synced Rename");
    }

    private void UpdateFolderAndEntries(AssetProvider provider)
    {
        var currentNode = assetBrowser.CurrentNode;

        var prevSize = currentNode.FolderCount * 64 +
                       currentNode.FileCount * 64;

        if (prevSize > 0) NameList.Slice(0, prevSize).Clear();

        int folderCount = currentNode.FolderCount, fileCount = currentNode.FileCount;
        if (folderCount + fileCount > MaxItems)
            throw new InvalidOperationException("Overflow, fix size management");

        var displayItems = _displayItems;
        for (var i = 0; i < folderCount; i++)
        {
            var name = currentNode.Children[i].FolderName;
            var offset = i > 0 ? displayItems[i - 1].NameHandle.End : 0;
            var written = NameList.SliceFrom(offset).Writer().Append(name).End();

            var fileId = new AssetFileId(-i);
            displayItems[i] = new FileDisplayItem(fileId, (offset, written.Length), FileSpecBinding.Unknown);
        }

        for (var i = 0; i < fileCount; i++)
        {
            var index = i + folderCount;
            var fileId = currentNode.FileIds[i];

            var name = provider.TryGetByRootFile(fileId, out var asset)
                ? asset.Name
                : provider.GetFile(fileId).LogicalName;

            var status = provider.GetFileBindingStatus(fileId);

            var offset = index > 0 ? displayItems[index - 1].NameHandle.End : 0;
            var written = NameList.SliceFrom(offset).Writer().Append(name).End();

            displayItems[index] = new FileDisplayItem(fileId, (offset, written.Length), status);
        }

        SetSearch(default);
    }

    public void SetSearch(ReadOnlySpan<byte> searchString)
    {
        var fileCount = assetBrowser.FileCount;
        var folderCount = assetBrowser.FolderCount;
        var totalCount = fileCount + folderCount;

        var searchIndices = _searchIndices.AsSpan();
        searchIndices.Clear();

        var count = 0;
        if (searchString.IsEmpty)
        {
            count = totalCount;
            for (var i = 0; i < totalCount; i++)
                searchIndices[i] = (byte)i;
        }
        else
        {
            var nameBuffer = stackalloc byte[NameLength];
            var name = new NativeView<byte>(nameBuffer, NameLength);

            for (var i = 0; i < totalCount; i++)
            {
                var fileName = GetName(i).AsSpan().SliceNullTerminate();
                var nameSpan = name.Writer().Append(fileName).EndSpan().ToLowerAscii();
                if (!nameSpan.StartsWith(searchString)) continue;
                searchIndices[count++] = (byte)i;
            }
        }

        PendingFilter = true;
        FilteredCount = count;
    }
}