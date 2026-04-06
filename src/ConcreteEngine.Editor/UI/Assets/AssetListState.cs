using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Extensions;

namespace ConcreteEngine.Editor.UI.Assets;

internal readonly struct FileDisplayItem(AssetFileId fileId, AssetId assetId, int nameLength, FileSpecBinding binding)
{
    public readonly AssetFileId FileId = fileId;
    public readonly AssetId AssetId = assetId;
    public readonly ushort NameLength = (ushort)nameLength;
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

    public NativeViewPtr<byte> NameList = NativeViewPtr<byte>.MakeNull();

    //
    private AssetKind CurrentKind => assetBrowser.CurrentKind;

    public NativeViewPtr<byte> GetName(int i) => NameList.Slice(i * NameLength, i * NameLength + NameLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* GetDrawData(byte i, out FileDisplayItem it)
    {
        it =  _displayItems[i];
        return NameList + i * NameLength;
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
            UpdateRename(renamedAsset);
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

        var prevSize = currentNode.FolderCount * 64 +
                       currentNode.FileCount * 64;

        if (prevSize > 0) NameList.Slice(0, prevSize).Clear();

        int folderCount = currentNode.FolderCount, fileCount = currentNode.FileCount;
        if (folderCount + fileCount > MaxItems)
            throw new InvalidOperationException("Overflow, fix size management");

        for (var i = 0; i < folderCount; i++)
        {
            var name = currentNode.Children[i].FolderName;
            GetName(i).Writer().Write(name);
            
            var fileId = new AssetFileId(-i);
            _displayItems[i] = new FileDisplayItem(fileId,default, name.Length, FileSpecBinding.Unknown);
        }

        for (var i = 0; i < fileCount; i++)
        {
            var fileId = currentNode.FileIds[i];
            var file = provider.GetFile(fileId);
            var status = provider.GetFileBindingStatus(fileId);

            var assetId = AssetId.Empty;
            if (status == FileSpecBinding.RootFile && provider.TryGetByRootFile(fileId, out var asset))
                assetId = asset.Id;
            else if (status == FileSpecBinding.UnboundFile)
                assetId = new AssetId(-1);
            

            var fileName = file.LogicalName;
            GetName(i + folderCount).Writer().Write(fileName);
            _displayItems[i + folderCount] = new FileDisplayItem(fileId, assetId, fileName.Length, status);
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
            var name = new NativeViewPtr<byte>(nameBuffer, NameLength);

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