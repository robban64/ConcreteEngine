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
    
    private AssetFileId[] _currentFileIds = new AssetFileId[64];
    private AssetFileId[] _filteredIds = new AssetFileId[64];

    public AssetBrowser()
    {
        RootNode = new AssetDirectoryNode("", null);
        CurrentNode = RootNode;
    }

    public int FolderCount => CurrentNode.FolderCount;
    public bool IsRootPath => RootNode == CurrentNode;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileId> GetFileIds() => _currentFileIds.AsSpan(0, FileCount);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileId> GetFilteredFileIds() => _filteredIds.AsSpan(0, FileCount);
    
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

        SetNode(RootNode.GetChildren()[0].GetChildren()[0]);
    }
    public void SetSearch(ReadOnlySpan<char> searchString)
    {
        int fileCount = FileCount, folderCount = FolderCount;

        var filteredIds = _filteredIds.AsSpan();
        
        if (searchString.IsEmpty)
        {
            _currentFileIds.CopyTo(filteredIds);
            FilteredCount = fileCount;
            return;
        }
        
        filteredIds.Clear();
        var count = 0;
        for (var i = 0; i < fileCount; i++)
        {
            var id = _currentFileIds[i];
            var name = AssetManager.FileRegistry.Get(id).LogicalName.AsSpan();
            if (!name.StartsWith(searchString)) continue;
            filteredIds[count++] = id;
        }

        FilteredCount = count;
    }
    
    private void SetNode(AssetDirectoryNode node)
    {
        CurrentNode = node;
        FileCount = 0;
        Array.Clear(_currentFileIds);

        if (!AssetManager.FileRegistry.TryGetDirectoryFileIds(node.Path, out var ids)) return;

        if (_currentFileIds.Length < ids.Length)
        {
            var newCapacity = CapacityUtils.CapacityGrowthToFit(_currentFileIds.Length, ids.Length);
            Array.Resize(ref _currentFileIds, newCapacity);
            Array.Resize(ref _filteredIds, newCapacity);
        }

        for (var i = 0; i < ids.Length; i++)
        {
            _currentFileIds[i] = ids[i];
            _filteredIds[i] = ids[i];
        }

        FileCount = ids.Length;
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
    
    internal sealed class AssetDirectoryNode(string path, AssetDirectoryNode? parent)
    {
        private readonly int _nameOffset = path.LastIndexOf('/') + 1;

        public readonly string Path = path;
        public readonly AssetDirectoryNode? Parent = parent;
        private readonly List<AssetDirectoryNode> _children = [];

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
        public ReadOnlySpan<char> GetRelativePath() => Path.AsSpan(EnginePath.AssetBasePath.Length);

        
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