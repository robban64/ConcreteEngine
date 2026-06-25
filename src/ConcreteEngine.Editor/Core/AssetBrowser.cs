using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Core.Engine.Configuration;

namespace ConcreteEngine.Editor.Core;


internal sealed class AssetBrowser
{
    public readonly AssetDirectoryNode RootNode;
    public AssetDirectoryNode CurrentNode { get; private set; }
    
    public int FileCount { get; private set; }
    public int FilteredCount { get; private set; }

    public FileBinding FileFilter = FileBinding.Unknown;
    public AssetStorage StorageFilter = AssetStorage.None;

    private AssetFileId[] _filteredFileIds = new AssetFileId[64];

    public AssetBrowser()
    {
        RootNode = new AssetDirectoryNode("", null);
        CurrentNode = RootNode;
    }

    public int FolderCount => CurrentNode.FolderCount;
    public bool IsRootPath => CurrentNode == RootNode || CurrentNode == RootNode.GetChild(0);

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public ReadOnlySpan<AssetFileId> GetFileIds() => _currentFileIds.AsSpan(0, FileCount);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileId> GetFilteredFileIds() => _filteredFileIds.AsSpan(0, FileCount);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseObjectEnumerator<AssetFileId,AssetFile> GetFilteredFileEnumerator() 
        => AssetManager.FileRegistry.MakeSparseEnumerator(GetFilteredFileIds());

    public void GoToChild(ReadOnlySpan<char> folderName)
    {
        ArgumentOutOfRangeException.ThrowIfZero(folderName.Length, nameof(folderName));
        var node = CurrentNode.FindChild(folderName);
        if (node is null) return;
        SetNode(node);
    }

    public void GoToParent()
    {
        if (IsRootPath || CurrentNode.Parent is not {} parent) return;
        SetNode(parent);
    }

    public void SetDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        if(CurrentNode.Path == directory) return;

        var node = RootNode.FindNodeByPath(directory);
        if (node is null) return;

        SetNode(node);
    }

    public void BuildFullDirectory()
    {
        foreach (var it in AssetManager.FileRegistry.GetDirectories())
            AddDirectory(RootNode, it);

        SetNode(RootNode.GetChild(0).GetChild(0));
    }
    public void SetSearch(ReadOnlySpan<char> searchString)
    {
        Array.Clear(_filteredFileIds);

        if(!AssetManager.FileRegistry.TryGetDirectoryFileIds(CurrentNode.Path, out var ids))
            Throwers.InvalidOperation(CurrentNode.Path);
        
        var fileFilter = FileFilter;
        var storageFilter = StorageFilter;

        var count = 0;
        foreach (var file in AssetManager.FileRegistry.MakeSparseEnumerator(ids))
        {
            if (searchString.Length > 0 && !file.LogicalName.StartsWith(searchString)) continue;
            if (storageFilter > 0 && storageFilter != file.Storage) continue;
            if (fileFilter > 0 && fileFilter != file.Binding) continue;

            _filteredFileIds[count++] = file.Id;
        }

        FilteredCount = count;
        FileCount = ids.Length;
    }
    
    private void SetNode(AssetDirectoryNode node)
    {
        CurrentNode = node;
        FileCount = 0;
        Array.Clear(_filteredFileIds);

        if (!AssetManager.FileRegistry.TryGetDirectoryFileIds(node.Path, out var ids)) return;

        if (_filteredFileIds.Length < ids.Length)
        {
            var newCapacity = CapacityUtils.CapacityGrowthToFit(_filteredFileIds.Length, ids.Length);
            Array.Resize(ref _filteredFileIds, newCapacity);
        }

        SetSearch(ReadOnlySpan<char>.Empty);
    }
    
    private static void AddDirectory(AssetDirectoryNode rootNode, string path)
    {
        var node = rootNode;
        var currentPath = path.AsSpan();
        while (true)
        {
            if (currentPath.Length <= 1) return;

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
                int intermediateLength = path.Length - currentPath.Length + index;
                var intermediatePath = path.AsSpan(0, intermediateLength);
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

        public readonly string Path;
        public readonly byte[] FolderU8;
        public readonly AssetDirectoryNode? Parent;
        private readonly List<AssetDirectoryNode> _children = [];

        public AssetDirectoryNode(string path, AssetDirectoryNode? parent)
        {
            _nameOffset = path.LastIndexOf('/') + 1;
            Path = path;
            Parent = parent;

            if (path.Length <= _nameOffset)
            {
                FolderU8 = Encoding.UTF8.GetBytes(path);
                return;
            }

            var folderSpan = path.AsSpan(_nameOffset);
            FolderU8 = new byte[Encoding.UTF8.GetByteCount(folderSpan)];
            Encoding.UTF8.GetBytes(folderSpan, FolderU8);
        }

        internal int TotalFolderNameLengthUtf8 { get; private set; }

        public int FolderCount => _children.Count;

        public void AddChild(AssetDirectoryNode child)
        {
            ArgumentNullException.ThrowIfNull(child);
            ArgumentOutOfRangeException.ThrowIfNotEqual(this, child.Parent);
            _children.Add(child);
            TotalFolderNameLengthUtf8 += Encoding.UTF8.GetByteCount(child.GetFolderName()) + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetFolderName() => Path.AsSpan(_nameOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetRelativePath()
        {
            var span = Path.AsSpan();
            if(span.StartsWith("AppContent/"))
                return Path.AsSpan(EnginePath.AssetBasePath.Length);
            return span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AssetDirectoryNode GetChild(int i) => _children[i];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<AssetDirectoryNode> GetChildren() => CollectionsMarshal.AsSpan(_children);


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