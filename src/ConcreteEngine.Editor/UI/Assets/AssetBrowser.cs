using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed class AssetDirectoryNode(string folderName)
{
    public readonly string FolderName = folderName;
    public readonly List<AssetFileId> FileIds = [];
    public readonly List<AssetDirectoryNode> Children = [];

    public AssetDirectoryNode? FindNodeByPath(ReadOnlySpan<char> path)
    {
        var node = this;
        while (true)
        {
            if (path.IsEmpty) return null;

            var index = path.IndexOf('/');
            var folder = index > 0 ? path.Slice(0, index) : path;

            var foundChild = node.FindChild(folder);
            if (foundChild is null) return null;

            if (index < 0)
                return foundChild;

            path = path.Slice(index + 1);
            node = foundChild;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetDirectoryNode? FindChild(ReadOnlySpan<char> folder)
    {
        foreach (var it in Children)
        {
            if (folder.SequenceEqual(it.FolderName)) return it;
        }

        return null;
    }
}

internal struct AssetFileDisplayItem(AssetFileId fileId, AssetId assetRootId, NativeViewPtr<byte> name)
{
    public readonly AssetFileId FileId = fileId;
    public readonly AssetId AssetRootId = assetRootId;
    public readonly Pointer<byte> Name = new(name);
    public readonly ulong PackedName = StringPacker.PackAscii(name.AsSpan(), true);

    public bool IsAssetRootFile
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => AssetRootId.IsValid();
    }
}

internal sealed unsafe class AssetBrowser
{
    public static int Capacity => (32 + 128) * AssetObject.MaxNameLength;
    public AssetKind CurrentKind { get; private set; } = AssetKind.Texture;
    public string CurrentDirectory { get; private set; } = string.Empty;

    public int FolderCount { get; private set; }
    public int AssetCount { get; private set; }
    public int FilteredCount { get; private set; }


    private readonly Pointer<byte>[] _subFolders = new Pointer<byte>[32];
    private readonly AssetFileDisplayItem[] _entries = new AssetFileDisplayItem[128];
    private readonly byte[] _searchIndices = new byte[128];
    
    private readonly AssetProvider _provider;
    private readonly AssetDirectoryNode _rootNode;
    private AssetDirectoryNode _currentNode;

    private NativeViewPtr<byte> _buffer = NativeViewPtr<byte>.MakeNull();

    public AssetBrowser(AssetProvider provider)
    {
        _provider = provider;
        _rootNode = new AssetDirectoryNode("assets");
        _currentNode = _rootNode;
    }

    public void SetBuffer(NativeViewPtr<byte> buffer)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(buffer.IsNull, true);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, 1024);
        _buffer = buffer;
    }


    public int TotalFilteredCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => FolderCount + FilteredCount;
    }

    public int TotalCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => AssetCount + FolderCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<Pointer<byte>> GetSubFolders() => _subFolders.AsSpan(0, FolderCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileDisplayItem> GetEntries() => _entries.AsSpan(0, AssetCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeSpan<byte> GetSearchIndices() => new(_searchIndices.AsSpan(0,FilteredCount));

    public void SetSearch(ulong searchKey, ulong searchMask)
    {
        _searchIndices.AsSpan().Clear();
        var len = AssetCount;
        if (searchKey == 0)
        {
            for (byte i = 0; i < len; i++) _searchIndices[i] = i;
            FilteredCount = len;
            return;
        }

        int count = 0;
        for (byte i = 0; i < len; i++)
        {
            var packedName = _entries[i].PackedName;
            if ((packedName & searchMask) != searchKey) continue;
            _searchIndices[count++] = i;
        }
        FilteredCount = count;

    }

    public void SetLocalDirectory(string folderName)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderName);
        if (CurrentKind == AssetKind.Unknown || CurrentDirectory.EndsWith(folderName)) return;

        var node = _currentNode.FindChild(folderName);
        if (node is null) return;

        _currentNode = node;
        CurrentDirectory = Path.Combine(CurrentDirectory, folderName);
        UpdateFolderAndEntries();
    }

    public void SetToParentDirectory()
    {
        if (CurrentKind == AssetKind.Unknown) return;
        var endIndex = CurrentDirectory.LastIndexOf('/');
        if (endIndex < 0) return;
        var newDirectory = CurrentDirectory.Substring(0, endIndex);

        var node = _rootNode.FindNodeByPath(newDirectory);
        if (node is null) return;

        CurrentDirectory = newDirectory;
        _currentNode = node;
        UpdateFolderAndEntries();
    }

    public void SetDirectory(string directory, AssetKind kind = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        if (kind > 0) CurrentKind = kind;

        if (CurrentKind == AssetKind.Unknown || CurrentDirectory == directory) return;

        var node = _rootNode.FindNodeByPath(directory);
        if (node is null) return;

        _currentNode = node;
        CurrentDirectory = directory;
        UpdateFolderAndEntries();
    }

    public void BuildFullDirectory()
    {
        foreach (var asset in _provider.GetAllAssets())
        {
            var file = _provider.GetAssetRootFile(asset.Id);
            AddFile(file, Path.GetDirectoryName(file.RelativePath.AsSpan()));
        }

        foreach (var fileId in _provider.GetUnboundFileIds())
        {
            var file = _provider.GetFileSpec(fileId);
            AddFile(file, Path.GetDirectoryName(file.RelativePath.AsSpan()));
        }
    }

    private void UpdateFolderAndEntries()
    {
        _entries.AsSpan().Clear();
        _subFolders.AsSpan().Clear();
        _searchIndices.AsSpan().Clear();

        var currentNode = _currentNode;
        var folderCount = FolderCount = currentNode.Children.Count;
        var fileCount = AssetCount = currentNode.FileIds.Count;
        
        var ptrIdx = 0;
        for (var i = 0; i < folderCount; i++)
        {
            var ptr = _buffer.SliceFrom(ptrIdx++ * AssetObject.MaxNameLength);
            _subFolders[i] = new Pointer<byte>(ptr);
            ptr.Writer().Write(currentNode.Children[i].FolderName);
        }
        for (var i = 0; i < fileCount; i++)
        {
            var fileId = currentNode.FileIds[i];
            var file = _provider.GetFileSpec(fileId);
            var assetId = _provider.TryGetByRootFile(fileId, out var asset) ? asset.Id : AssetId.Empty;
            
            var ptr = _buffer.SliceFrom(ptrIdx++ * AssetObject.MaxNameLength);
            ptr.Writer().Write(file.LogicalName);
            _entries[i] = new AssetFileDisplayItem(fileId, assetId, ptr);
        }

        SetSearch(0, 0);
    }

    private void AddFile(AssetFileSpec file, ReadOnlySpan<char> path)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Storage != AssetStorageKind.FileSystem) return;

        var node = _rootNode;
        while (true)
        {
            if (path.IsEmpty) return;

            var index = path.IndexOf('/');
            var folder = index > 0 ? path.Slice(0, index) : path;

            var foundChild = node.FindChild(folder);
            if (foundChild is null)
            {
                foundChild = new AssetDirectoryNode(folder.ToString());
                node.Children.Add(foundChild);
            }

            if (index < 0)
            {
                foundChild.FileIds.Add(file.Id);
                return;
            }

            path = path.Slice(index + 1);
            node = foundChild;
        }
    }
}