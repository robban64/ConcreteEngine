using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Core.Engine.Configuration;

namespace ConcreteEngine.Editor.Core;

internal sealed class AssetDirectoryNode(string path,AssetDirectoryNode? parent)
{
    public readonly string Path = path;
    public readonly AssetDirectoryNode? Parent = parent;
    public readonly List<AssetDirectoryNode> Children = [];

    public int FolderCount => Children.Count;

    public void AddChild(AssetDirectoryNode child)
    {
        ArgumentNullException.ThrowIfNull(child);
        ArgumentOutOfRangeException.ThrowIfNotEqual(this, child.Parent);
        Children.Add(child);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetDirectoryNode> GetChildren() => CollectionsMarshal.AsSpan(Children);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> GetFolderName()
    {
        var span = Path.AsSpan();
        return span.Slice(span.LastIndexOf('/') + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileId> GetFileIds()
    {
        return AssetManager.FileRegistry.TryGetDirectoryFileIds(Path, out var ids) ? ids : default;
    }

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

internal sealed class AssetBrowser
{
    public readonly AssetDirectoryNode RootNode;
    public AssetDirectoryNode CurrentNode { get; private set; }

    public AssetBrowser()
    {
        RootNode = new AssetDirectoryNode("", null);
        CurrentNode = RootNode;
    }

    public int FolderCount => CurrentNode.FolderCount;
    public bool IsRootPath => RootNode == CurrentNode;

    public void GoToChild(ReadOnlySpan<char> folderName)
    {
        ArgumentOutOfRangeException.ThrowIfZero(folderName.Length, nameof(folderName));
        var node = CurrentNode.FindChild(folderName);
        if (node is null) return;
        CurrentNode = node;
    }

    public void GoToParent()
    {
        if (IsRootPath || CurrentNode.Parent is not {} parent) return;
        CurrentNode = parent;
    }

    public void SetDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        if(CurrentNode.Path == directory) return;

        var node = RootNode.FindNodeByPath(directory);
        if (node is null) return;

        CurrentNode = node;
    }

    public void BuildFullDirectory()
    {
        foreach (var it in AssetManager.FileRegistry.GetDirectories())
            AddDirectory(RootNode, it);
    }
    
    private void AddDirectory(AssetDirectoryNode rootNode, string path)
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
                if (RootNode.FindNodeByPath(intermediatePath) == null)
                {
                    var intermediateNode = new AssetDirectoryNode(intermediatePath.ToString(), node);
                    node.AddChild(intermediateNode);
                    node = intermediateNode;
                }
            
            }
            currentPath = currentPath.Slice(index + 1);
        }
    }
    
}