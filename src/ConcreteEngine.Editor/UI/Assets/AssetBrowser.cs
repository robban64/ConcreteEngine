using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Editor.Utils;
using static ConcreteEngine.Core.Engine.Assets.AssetManager;

namespace ConcreteEngine.Editor.UI;

internal sealed class AssetBrowser
{
    private const int Capacity = 64;

    public int FileCount { get; private set; }
    public int FilteredCount { get; private set; }

    public readonly AssetDirectoryNode RootNode;
    public AssetDirectoryNode CurrentNode { get; private set; }

    private AssetFileId[] _filteredFileIds;
    private readonly CircularListBuffer<FileItem> _fileListBuffer;

    private readonly Action<AssetBrowser> _onDirectoryChange;


    public AssetBrowser(Action<AssetBrowser> onDirectoryChange)
    {
        _onDirectoryChange = onDirectoryChange;
        _filteredFileIds = new AssetFileId[64];
        _fileListBuffer = new CircularListBuffer<FileItem>(Capacity, OnInvalidateDrawBuffer);

        RootNode = new AssetDirectoryNode("", null);
        CurrentNode = RootNode;
    }


    public string CurrentPath => CurrentNode.Path;
    public int FolderCount => CurrentNode.FolderCount;
    public bool IsRootPath => CurrentNode == RootNode || CurrentNode == RootNode.GetChild(0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CircularListBuffer<FileItem>.Enumerator GetDrawEnumerator(int start, int length) =>
        _fileListBuffer.GetView(start, length);

    //

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
        if (CurrentPath == directory) return;

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

    private void SetNode(AssetDirectoryNode node)
    {
        CurrentNode = node;
        FilteredCount = FileCount = FileRegistry.GetDirectoryIds(CurrentPath).Length;
        _onDirectoryChange(this);
    }

    //
    public void Commit(ReadOnlySpan<char> searchText, FileBinding bindingFilter, AssetKind assetFilter)
    {
        var ids = FileRegistry.GetDirectoryIds(CurrentNode.Path);
        if(_filteredFileIds.Length < ids.Length)
            _filteredFileIds = new AssetFileId[CapacityUtils.CapacityGrowthToFit(_filteredFileIds.Length, ids.Length)];
        else 
            Array.Clear(_filteredFileIds);

        
        var count = 0;
        foreach (var file in FileRegistry.MakeSparseEnumerator(ids))
        {
            if (assetFilter != AssetKind.Unknown)
            {
                if (!file.AssetRootId.IsValid() || assetFilter != Assets.Get<AssetObject>(file.AssetRootId).Kind)
                    continue;
            }

            if (bindingFilter != FileBinding.Unknown && file.Binding != bindingFilter) continue;

            if (searchText.Length > 0 && !file.LogicalName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
                continue;

            _filteredFileIds[count++] = file.Id;
        }

        FilteredCount = count;

        _fileListBuffer.Invalidate(0, _fileListBuffer.Capacity);
    }
    

    private void OnInvalidateDrawBuffer(int start, Span<FileItem> span)
    {
        var count = 0;
        var fileIds = new ReadOnlySpan<AssetFileId>(_filteredFileIds, start, span.Length);
        foreach (var file in FileRegistry.MakeSparseEnumerator(fileIds))
        {
            var kind = AssetKind.Unknown;
            if (file.AssetRootId.IsValid()) kind = Assets.Get<AssetObject>(file.AssetRootId).Kind;

            span[count++] = new FileItem(file.LogicalName, file.Id, file.Binding, file.Storage, kind);
        }
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