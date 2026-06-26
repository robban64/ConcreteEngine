using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using static ConcreteEngine.Core.Engine.Assets.AssetManager;

namespace ConcreteEngine.Editor.Core;

internal readonly struct FileItem : IComparable<FileItem>
{
    public readonly string Name;
    public readonly AssetFileId Id;
    public readonly uint Icon;
    public readonly uint Color;
    public readonly FileBinding Binding;
    public readonly AssetStorage Storage;
    public readonly AssetKind AssetKind;

    public FileItem(string name, AssetFileId id, FileBinding binding, AssetStorage storage, AssetKind assetKind)
    {
        Name = name;
        Id = id;
        Binding = binding;
        Storage = storage;
        AssetKind = assetKind;
        AssetsExtensions.GetIconAndColor(binding, assetKind, out Icon, out Color);
    }

    public int CompareTo(FileItem other)
    {
        var c = ((int)Binding).CompareTo((int)other.Binding);
        return c != 0 ? c : Name.CompareTo(other.Name, StringComparison.Ordinal);
    }

}

internal sealed class AssetBrowser
{
    public readonly AssetDirectoryNode RootNode;
    public AssetDirectoryNode CurrentNode { get; private set; }

    public int FileCount { get; private set; }
    public int FilteredCount { get; private set; }

    public FileBinding FileFilter = FileBinding.Unknown;
    public AssetStorage StorageFilter = AssetStorage.None;

    private AssetFileId[] _filteredFileIds = new AssetFileId[64];
    private FileItem[] _items = new FileItem[64];

    private readonly Action<AssetBrowser> _onDirectoryChange;

    public AssetBrowser(Action<AssetBrowser> onDirectoryChange)
    {
        _onDirectoryChange = onDirectoryChange;
        RootNode = new AssetDirectoryNode("", null);
        CurrentNode = RootNode;
    }

    public int FolderCount => CurrentNode.FolderCount;
    public bool IsRootPath => CurrentNode == RootNode || CurrentNode == RootNode.GetChild(0);

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public ReadOnlySpan<AssetFileId> GetFileIds() => _currentFileIds.AsSpan(0, FileCount);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<FileItem> GetFileItems(int start, int length)
        => _items.AsSpan(start, int.Min(length, FilteredCount - start));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileId> GetFilteredFileIds() => _filteredFileIds.AsSpan(0, FilteredCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseObjectEnumerator<AssetFileId, AssetFile> GetFilteredFileEnumerator()
        => FileRegistry.MakeSparseEnumerator(GetFilteredFileIds());

    public void GoToChild(ReadOnlySpan<char> folderName)
    {
        ArgumentOutOfRangeException.ThrowIfZero(folderName.Length, nameof(folderName));
        var node = CurrentNode.FindChild(folderName);
        if (node is null) return;
        SetNode(node);
    }

    public void GoToParent()
    {
        if (IsRootPath || CurrentNode.Parent is not { } parent) return;
        SetNode(parent);
    }

    public void SetDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        if (CurrentNode.Path == directory) return;

        var node = RootNode.FindNodeByPath(directory);
        if (node is null) return;

        SetNode(node);
    }

    public void BuildFullDirectory()
    {
        foreach (var it in FileRegistry.GetDirectories())
            AddDirectory(RootNode, it);

        SetNode(RootNode.GetChild(0).GetChild(0));
    }

    public void SetSearch(ReadOnlySpan<char> searchString)
    {
        Array.Clear(_filteredFileIds);
        Array.Clear(_items);

        if (!FileRegistry.TryGetDirectoryIds(CurrentNode.Path, out var ids))
        {
            FilteredCount = 0;
            return;
        }

        var fileFilter = FileFilter;
        var storageFilter = StorageFilter;

        var count = 0;
        foreach (var file in FileRegistry.MakeSparseEnumerator(ids))
        {
            if (searchString.Length > 0 && !file.LogicalName.StartsWith(searchString)) continue;
            if (storageFilter > 0 && storageFilter != file.Storage) continue;
            if (fileFilter > 0 && fileFilter != file.Binding) continue;

            var kind = AssetKind.Unknown;
            if (FileRegistry.TryGetByRootId(file.Id, out var assetId))
                kind = Assets.Get<AssetObject>(assetId).Kind;
            
            _filteredFileIds[count] = file.Id;
            _items[count++] = new FileItem(file.LogicalName, file.Id, file.Binding, file.Storage, kind);
        }
        _items.AsSpan(0, count).Sort();

        _filteredFileIds.AsSpan(0, count)
            .Sort(static (a, b) => ((int)FileRegistry.Get(a).Binding).CompareTo((int)FileRegistry.Get(b).Binding));

        FilteredCount = count;
    }

    private void SetNode(AssetDirectoryNode node)
    {
        CurrentNode = node;

        if (FileRegistry.TryGetDirectoryIds(node.Path, out var ids) && _filteredFileIds.Length < ids.Length)
        {
            var newCapacity = CapacityUtils.CapacityGrowthToFit(_filteredFileIds.Length, ids.Length);
            Array.Resize(ref _filteredFileIds, newCapacity);
        }

        FileCount = ids.Length;
        _onDirectoryChange(this);
    }

    private static void AddDirectory(AssetDirectoryNode rootNode, string path)
    {
        var node = rootNode;
        var currentPath = path.AsSpan();
        while (currentPath.Length > 1)
        {
            var index = currentPath.IndexOf('/');
            if (index < 0)
            {
                var newChild = new AssetDirectoryNode(path, node);
                node.AddChild(newChild);
                return;
            }

            var folder = currentPath.Slice(0, index);
            var foundChild = node.FindChild(folder);
            if (foundChild is not null)
            {
                node = foundChild;
            }
            else
            {
                var intermediatePath = path.AsSpan(0, path.Length - currentPath.Length + index);
                if (rootNode.FindNodeByPath(intermediatePath) == null)
                {
                    var intermediateNode = new AssetDirectoryNode(intermediatePath.ToString(), node);
                    node.AddChild(intermediateNode);
                    node = intermediateNode;
                }
            }

            currentPath = currentPath.Slice(index + 1);
        }
    }
    

    internal sealed class AssetDirectoryNode
    {
        private readonly int _nameOffset;
        public readonly bool IsFileSystem;

        public readonly string Path;
        public readonly AssetDirectoryNode? Parent;
        private readonly List<AssetDirectoryNode> _children = [];

        public readonly String32Utf8 PreviewName;
        
        public AssetDirectoryNode(string path, AssetDirectoryNode? parent)
        {
            Path = path;
            Parent = parent;
            
            IsFileSystem = path.StartsWith(EnginePath.AssetBasePath);
            var nameOffset = _nameOffset = path.LastIndexOf('/') + 1;
            PreviewName = new String32Utf8(path.AsSpan(nameOffset));
        }


        public int FolderCount => _children.Count;

        public void AddChild(AssetDirectoryNode child)
        {
            ArgumentNullException.ThrowIfNull(child);
            ArgumentOutOfRangeException.ThrowIfNotEqual(this, child.Parent);
            _children.Add(child);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetFolderName() => Path.AsSpan(_nameOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetRelativePath() => Path.AsSpan(EnginePath.AssetBasePathOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AssetDirectoryNode GetChild(int i) => _children[i];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<AssetDirectoryNode> GetChildren() => CollectionsMarshal.AsSpan(_children);


        public AssetDirectoryNode? FindNodeByPath(ReadOnlySpan<char> path)
        {
            var node = this;
            while (path.Length > 0)
            {
                var index = path.IndexOf('/');
                var folder = index > 0 ? path.Slice(0, index) : path;

                var foundChild = node.FindChild(folder);
                if (foundChild is null) return null;

                if (index < 0) return foundChild;

                path = path.Slice(index + 1);
                node = foundChild;
            }

            return null;
        }

        public AssetDirectoryNode? FindChild(ReadOnlySpan<char> folder)
        {
            foreach (var child in GetChildren())
            {
                if (folder.SequenceEqual(child.GetFolderName())) return child;
            }

            return null;
        }
    }
}