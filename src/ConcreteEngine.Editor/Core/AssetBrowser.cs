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
    
    private FileListItem[] _currentFileIds; 

    public AssetBrowser()
    {
        RootNode = new AssetDirectoryNode("", null);
        CurrentNode = RootNode;
        _currentFileIds = new FileListItem[64];
    }

    public int FolderCount => CurrentNode.FolderCount;
    public bool IsRootPath => RootNode == CurrentNode;
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<FileListItem> GetFileIds()
    {
        if((uint)FileCount > (uint)_currentFileIds.Length) 
            Throwers.IndexOutOfRange(nameof(_currentFileIds),FileCount, _currentFileIds.Length);
        
        return _currentFileIds.AsSpan(0, FileCount);
    }

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

    private void SetNode(AssetDirectoryNode node)
    {
        CurrentNode = node;
        Array.Clear(_currentFileIds);

        if (!AssetManager.FileRegistry.TryGetDirectoryFileIds(node.Path, out var ids))
        {
            FileCount = 0;
            return;
        }

        if (_currentFileIds.Length < ids.Length)
        {
            var newCapacity = CapacityUtils.CapacityGrowthToFit(_currentFileIds.Length, ids.Length);
            Array.Resize(ref _currentFileIds, newCapacity);
        }

        for (var i = 0; i < ids.Length; i++)
        {
            var fileId = ids[i];
            var file = AssetManager.FileRegistry.Get(fileId);
            _currentFileIds[i] = new FileListItem(file.Id, file.LogicalName, file.Binding, file.Storage);
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
    
    internal readonly struct FileListItem(AssetFileId fileId, string name, FileBinding binding, AssetStorage storage)
    {
        public readonly string Name = name;
        public readonly AssetFileId FileId = fileId;
        public readonly FileBinding Binding = binding;
        public readonly AssetStorage Storage = storage;
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
            TotalFolderNameLengthUtf8 += Encoding.UTF8.GetByteCount(child.GetFolderName());
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<AssetDirectoryNode> GetChildren() => CollectionsMarshal.AsSpan(_children);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetFolderName() => Path.AsSpan(_nameOffset);

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