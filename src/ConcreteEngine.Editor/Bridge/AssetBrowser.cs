using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Editor.Bridge;

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
    public AssetFileId FileId = fileId;
    public AssetId AssetRootId = assetRootId;
    public string Name = name;
    public string RelativePath = relativePath;

    public bool IsAssetRootFile => AssetRootId.IsValid();
}

internal sealed class AssetBrowser(AssetProvider provider)
{
    public AssetKind CurrentKind { get; private set; } = AssetKind.Texture;
    public string CurrentDirectory { get; private set; } = string.Empty;

    private readonly AssetDirectoryNode _rootNode = new("assets");

    private readonly List<string> _subFolders = new(8);
    private readonly List<AssetFileDisplayItem> _entries = new(64);
    
    public int FolderCount => _subFolders.Count;
    public int AssetCount => _entries.Count;
    
    public int TotalCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _entries.Count + _subFolders.Count ;
    }

    public ReadOnlySpan<string> GetSubFolders() => CollectionsMarshal.AsSpan(_subFolders);
    public ReadOnlySpan<AssetFileDisplayItem> GetEntries() => CollectionsMarshal.AsSpan(_entries);


    public void SetDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        if (CurrentKind == AssetKind.Unknown || CurrentDirectory == directory) return;

        var node = _rootNode.FindNodeByPath(directory);
        if (node is null) return;

        CurrentDirectory = directory;
        _entries.Clear();
        _subFolders.Clear();

        foreach (var fileId in node.FileIds)
        {
            var file = provider.GetFileSpec(fileId);
            var assetId = provider.TryGetByRootFile(fileId, out var asset) ? asset.Id : AssetId.Empty;
            _entries.Add(new AssetFileDisplayItem(fileId, assetId, file.LogicalName, file.RelativePath));
        }

        foreach (var it in node.Children)
            _subFolders.Add(it.FolderName);
    }

    public void BuildFullDirectory()
    {
        foreach (var asset in provider.GetAllAssets())
        {
            var file = provider.GetAssetRootFile(asset.Id);
            AddFile(file, Path.GetDirectoryName(file.RelativePath.AsSpan()));
        }

        foreach (var fileId in provider.GetUnboundFileIds())
        {
            var file = provider.GetFileSpec(fileId);
            AddFile(file, Path.GetDirectoryName(file.RelativePath.AsSpan()));
        }
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