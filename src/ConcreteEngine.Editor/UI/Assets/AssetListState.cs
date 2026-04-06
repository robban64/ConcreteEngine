using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Extensions;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed unsafe class AssetListState(AssetBrowser assetBrowser, AssetKind pendingKind)
{
    public const string GoBackString = "..";
    public const int MaxFolderCount = 32;
    public const int MaxAssetCount = 128;

    private const int FolderEndAt = String64Utf8.Capacity * MaxFolderCount;

    public static int Capacity => FolderEndAt + (MaxAssetCount * AssetFileDisplayItem.SizeOf);

    //
    public AssetFileId SelectedFileId { get; set; }
    public AssetKind PendingKind { get; private set; } = pendingKind;
    public bool PendingFilter { get; private set; } = false;
    public string? PendingDirectory { get; private set; }
    public int FilteredCount { get; private set; }

    private readonly byte[] _searchIndices = new byte[128];

    public NativeViewPtr<byte> ListBuffer = NativeViewPtr<byte>.MakeNull();

    //
    private AssetKind CurrentKind => assetBrowser.CurrentKind;

    public String64Utf8* SubFolderPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (String64Utf8*)ListBuffer.Ptr;
    }

    public AssetFileDisplayItem* FileItemPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (AssetFileDisplayItem*)(ListBuffer.Ptr + FolderEndAt);
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
            UpdateRename();
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

    private void UpdateRename()
    {
        var currentNode = assetBrowser.CurrentNode;
        var fileCount = currentNode.FileCount;

        for (var i = 0; i < fileCount; i++)
        {
            var fileId = currentNode.FileIds[i];
            if (EngineObjectStore.AssetProvider.TryGetByRootFile(fileId, out var asset))
                FileItemPtr[i].SetName(asset.Name);
        }

        Console.WriteLine("AssetList Synced Rename");
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

    private void UpdateFolderAndEntries(AssetProvider provider)
    {
        var currentNode = assetBrowser.CurrentNode;

        var prevLen = currentNode.FolderCount * String64Utf8.Capacity +
                      currentNode.FileCount * AssetFileDisplayItem.SizeOf;

        if (prevLen > 0) ListBuffer.AsSpan(0, prevLen).Clear();

        int folderCount = currentNode.FolderCount, fileCount = currentNode.FileCount;
        if (folderCount > MaxFolderCount || fileCount > MaxAssetCount)
            throw new InvalidOperationException("Overflow, fix size management");

        var ptrIdx = 0;
        for (var i = 0; i < folderCount; i++)
        {
            var ptr = (String64Utf8*)ListBuffer.Ptr;
            ptr[i] = new String64Utf8(currentNode.Children[i].FolderName);
        }

        var displayItems = FileItemPtr;
        for (var i = 0; i < fileCount; i++)
        {
            var fileId = currentNode.FileIds[i];
            var file = provider.GetFile(fileId);
            var status = provider.GetFileBindingStatus(fileId);

            var assetId = AssetId.Empty;
            if (status == FileSpecBinding.RootFile)
            {
                provider.TryGetByRootFile(fileId, out var asset);
                assetId = asset.Id;
            }
            else if (status == FileSpecBinding.UnboundFile)
            {
                assetId = new AssetId(-1);
            }


            displayItems[i] = new AssetFileDisplayItem(fileId, assetId, file.LogicalName);
        }

        SetSearch(0, 0);
    }

    public void SetSearch(ulong searchKey, ulong searchMask)
    {
        var fileItems = FileItemPtr;
        var fileCount = assetBrowser.FileCount;
        var searchIndices = _searchIndices.AsSpan();
        searchIndices.Clear();

        short count = 0;
        if (searchKey == 0)
        {
            count = (short)fileCount;
            for (byte i = 0; i < fileCount; i++)
                searchIndices[i] = i;
        }
        else
        {
            for (byte i = 0; i < fileCount; i++)
            {
                var packedName = fileItems[i].PackedName;
                if ((packedName & searchMask) != searchKey) continue;
                searchIndices[count++] = i;
            }
        }

        PendingFilter = true;
        FilteredCount = count;
    }
}