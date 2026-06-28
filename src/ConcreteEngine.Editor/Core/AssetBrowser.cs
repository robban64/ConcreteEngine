using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using static ConcreteEngine.Core.Engine.Assets.AssetManager;

namespace ConcreteEngine.Editor.Core;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct FileItem(
    string name,
    AssetFileId id,
    FileBinding binding,
    AssetStorage storage,
    AssetKind assetKind) : IComparable<FileItem>
{
    public readonly AssetFileId Id = id;
    public readonly FileBinding Binding = binding;
    public readonly AssetStorage Storage = storage;
    public readonly AssetKind AssetKind = assetKind;

    public readonly String16Utf8 DisplayName = name;

    public int CompareTo(FileItem other)
    {
        var c = ((int)Binding).CompareTo((int)other.Binding);
        return c != 0 ? c : DisplayName.GetTextSpan().SequenceCompareTo(other.DisplayName.GetTextSpan());
    }
}

internal sealed class AssetBrowser
{
    public readonly AssetDirectoryNode RootNode;
    public AssetDirectoryNode CurrentNode { get; private set; }

    public int FileCount { get; private set; }
    public int FilteredCount { get; private set; }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<FileItem> GetFileItems(int start, int length) => new(_items, start, length);

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
        Array.Clear(_items);

        if (!FileRegistry.TryGetDirectoryIds(CurrentNode.Path, out var ids))
        {
            FilteredCount = 0;
            return;
        }

        var count = 0;
        foreach (var file in FileRegistry.MakeSparseEnumerator(ids))
        {
            if (file.Binding == FileBinding.DependentFile) continue;
            if (searchString.Length > 0 && !file.LogicalName.StartsWith(searchString)) continue;

            var kind = AssetKind.Unknown;
            if (file.AssetRootId.IsValid())
                kind = Assets.Get<AssetObject>(file.AssetRootId).Kind;

            _items[count++] = new FileItem(file.LogicalName, file.Id, file.Binding, file.Storage, kind);
        }

        _items.AsSpan(0, count).Sort();
        FilteredCount = count;
    }

    private void SetNode(AssetDirectoryNode node)
    {
        CurrentNode = node;

        if (FileRegistry.TryGetDirectoryIds(node.Path, out var ids) && _items.Length < ids.Length)
        {
            var newCapacity = CapacityUtils.CapacityGrowthToFit(_items.Length, ids.Length);
            Array.Resize(ref _items, newCapacity);
        }

        FilteredCount = FileCount = ids.Length;
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
        public ReadOnlySpan<char> GetRelativePath() =>
            IsFileSystem ? Path.AsSpan(EnginePath.AssetBasePathOffset) : Path.AsSpan();

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