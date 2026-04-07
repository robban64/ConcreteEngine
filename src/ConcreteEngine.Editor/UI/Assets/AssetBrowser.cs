using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Extensions;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed class AssetDirectoryNode(string folderName)
{
    public readonly string FolderName = folderName;
    public readonly List<AssetFileId> FileIds = [];
    public readonly List<AssetDirectoryNode> Children = [];

    public int FileCount => FileIds.Count;
    public int FolderCount => Children.Count;


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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetDirectoryNode? FindChild(AssetFileId fileId)
    {
        foreach (var it in Children)
        {
            if (it.FileIds.Contains(fileId)) return it;
        }

        return null;
    }
}

internal sealed class AssetBrowser
{
    public string CurrentDirectory
    {
        get;
        private set
        {
            IsRootPath = CurrentKind.ToRootFolder() == value;
            field = value;
        }
    } = string.Empty;

    public AssetKind CurrentKind { get; private set; }
    public bool IsRootPath { get; private set; }

    public AssetDirectoryNode CurrentNode { get; private set; }
    public readonly AssetDirectoryNode RootNode;


    public AssetBrowser()
    {
        RootNode = new AssetDirectoryNode("assets");
        CurrentNode = RootNode;
    }

    public int FolderCount => CurrentNode.FolderCount;
    public int FileCount => CurrentNode.FileCount;


    public string GetChildFolderName(int index)
    {
        if ((uint)index >= (uint)CurrentNode.Children.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        return CurrentNode.Children[index].FolderName;
    }

    public void SetLocalDirectory(string folderName)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderName);
        if (CurrentKind == AssetKind.Unknown || CurrentDirectory.EndsWith(folderName)) return;

        var node = CurrentNode.FindChild(folderName);
        if (node is null) return;

        CurrentNode = node;
        CurrentDirectory = Path.Join(CurrentDirectory, folderName);
    }

    public void SetToParentDirectory()
    {
        if (CurrentKind == AssetKind.Unknown) return;
        var endIndex = CurrentDirectory.LastIndexOf('/');
        if (endIndex < 0) return;
        var newDirectory = CurrentDirectory.Substring(0, endIndex);

        var node = RootNode.FindNodeByPath(newDirectory);
        if (node is null) return;

        CurrentDirectory = newDirectory;
        CurrentNode = node;
    }

    public void SetDirectory(string directory, AssetKind kind = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        if (kind > 0) CurrentKind = kind;

        if (CurrentKind == AssetKind.Unknown || CurrentDirectory == directory) return;

        var node = RootNode.FindNodeByPath(directory);
        if (node is null) return;


        CurrentNode = node;
        CurrentDirectory = directory;
    }

    public void BuildFullDirectory()
    {
        var assetProvider = EngineObjectStore.AssetProvider;

        var addedFiles = new HashSet<int>(assetProvider.FileCount);

        for (var i = 1; i < EnumCache<AssetKind>.Count; i++)
            AddAssetFilesFor((AssetKind)i, assetProvider, addedFiles);

        foreach (var fileId in assetProvider.GetUnboundFileIds())
        {
            var file = assetProvider.GetFile(fileId);
            AddFile(file, Path.GetDirectoryName(file.RelativePath.AsSpan()));
        }

        return;

        void AddAssetFilesFor(AssetKind kind, AssetProvider provider, HashSet<int> filesAdded)
        {
            foreach (var assetId in provider.GetAssetIdsByKind(kind))
            {
                var file = provider.GetAssetRootFile(assetId);
                if (!filesAdded.Add(file.Id) && file.Storage == AssetStorageKind.FileSystem)
                    throw new InvalidOperationException();
                AddFile(file, Path.GetDirectoryName(file.RelativePath.AsSpan()));
            }

            foreach (var assetId in provider.GetAssetIdsByKind(kind))
            {
                var fileIds = provider.GetAssetFileBindings(assetId);
                if (fileIds.Length <= 1) continue;
                foreach (var fileId in fileIds)
                {
                    if (!filesAdded.Add(fileId)) continue;
                    var file = provider.GetFile(fileId);
                    AddFile(file, Path.GetDirectoryName(file.RelativePath.AsSpan()));
                }
            }
        }
    }

    private void AddFile(AssetFileSpec file, ReadOnlySpan<char> path)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Storage != AssetStorageKind.FileSystem) return;

        var node = RootNode;
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