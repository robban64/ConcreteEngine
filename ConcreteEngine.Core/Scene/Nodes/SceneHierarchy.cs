namespace ConcreteEngine.Core.Scene.Nodes;

internal class SceneHierarchy
{
    private readonly List<SceneNode> _roots = new();
    private readonly Dictionary<string, SceneNode> _nodeDict = new();

    public IReadOnlyList<SceneNode> Roots => _roots;

    //public int NodeCount { get; private set; }

    public bool TryGetNode(string name, out SceneNode node) => _nodeDict.TryGetValue(name, out node!);

    public void AddRoot(SceneNode node)
    {
        ValidateRootNode(node);
        node.Parent = null;
        _roots.Add(node);
        _nodeDict.Add(node.Name, node);
    }

    public void AddChild(SceneNode parent, SceneNode child)
    {
        child.Parent?.RemoveChild(child);
        child.Parent = parent;
        parent.AddChild(child);
    }

    public void RemoveNode(SceneNode node)
    {
        if (node.IsRoot) _roots.Remove(node);
        else node.Parent!.RemoveChild(node);
        RemoveRecursive(node);
    }

    // Remove orphans from the node dict
    private void RemoveRecursive(SceneNode node)
    {
        _nodeDict.Remove(node.Name);
        foreach (var c in node.Children) RemoveRecursive(c);
    }

    private void ValidateRootNode(SceneNode node)
    {
    }
}