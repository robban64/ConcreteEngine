using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

internal sealed class AssetFileDisplayItem(AssetFileId fileId, AssetId assetRootId, string name, string relativePath)
{
    public readonly AssetFileId FileId = fileId;
    public readonly AssetId AssetRootId = assetRootId;
    public readonly string Name = name;
    public readonly string RelativePath = relativePath;
    public readonly ulong PackedName = StringPacker.PackAscii(name.AsSpan(), true);

    public bool IsAssetRootFile
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => AssetRootId.IsValid();
    }
}

internal sealed class AssetBrowser
{
    public AssetKind CurrentKind { get; private set; } = AssetKind.Texture;
    public string CurrentDirectory { get; private set; } = string.Empty;

    private AssetDirectoryNode _currentNode;
    private readonly AssetDirectoryNode _rootNode;

    private readonly List<string> _subFolders = new(8);
    private readonly List<AssetFileDisplayItem> _entries = new(64);
    private readonly List<int> _searchIndices = new(64);
    private readonly AssetProvider _provider;

    public AssetBrowser(AssetProvider provider)
    {
        _provider = provider;
        _rootNode = new AssetDirectoryNode("assets");
        _currentNode = _rootNode;
    }

    public int FolderCount => _subFolders.Count;
    public int AssetCount => _entries.Count;
    public int FilteredCount => _searchIndices.Count;

    public int TotalCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _entries.Count + _subFolders.Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<string> GetSubFolders() => CollectionsMarshal.AsSpan(_subFolders);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileDisplayItem> GetEntries() => CollectionsMarshal.AsSpan(_entries);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<int> GetSearchIndices() => CollectionsMarshal.AsSpan(_searchIndices);

    public void SetSearch(ulong searchKey, ulong searchMask)
    {
        _searchIndices.Clear();
        if (searchKey == 0)
        {
            for (var i = 0; i < _entries.Count; i++)
                _searchIndices.Add(i);
            return;
        }

        for (var i = 0; i < _entries.Count; i++)
        {
            var packedName = _entries[i].PackedName;
            if ((packedName & searchMask) != searchKey) continue;
            _searchIndices.Add(i);
        }
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
        _entries.Clear();
        _subFolders.Clear();
        _searchIndices.Clear();

        foreach (var fileId in _currentNode.FileIds)
        {
            var file = _provider.GetFileSpec(fileId);
            var assetId = _provider.TryGetByRootFile(fileId, out var asset) ? asset.Id : AssetId.Empty;
            _entries.Add(new AssetFileDisplayItem(fileId, assetId, file.LogicalName, file.RelativePath));
        }

        foreach (var it in _currentNode.Children)
            _subFolders.Add(it.FolderName);

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