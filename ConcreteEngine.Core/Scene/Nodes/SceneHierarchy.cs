using ConcreteEngine.Core.Transforms;

namespace ConcreteEngine.Core.Scene.Nodes;


internal class SceneHierarchy
{
    private readonly List<SceneNode> _roots = new();
    private readonly Dictionary<string, SceneNode> _nodeDict = new();

    public IReadOnlyList<SceneNode> Roots => _roots;
    
    public int NodeCount { get; private set; }

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
        if(node.IsRoot) _roots.Remove(node);
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
    
    
    private class SceneHierarchyProcessor
    {
        private readonly Queue<SceneNode> _traverseQueue = new();

        public void ProcessNodes(IReadOnlyList<SceneNode> roots)
        {
            foreach (var root in roots)
            {
                if(root.Enabled) _traverseQueue.Enqueue(root);
            }
                

            while (_traverseQueue.Count > 0)
            {
                var node = _traverseQueue.Dequeue();
                if(!node.Enabled) continue;

                var parentTransform = node.Parent is null ? Transform2D.Identity : node.Parent.LocalTransform;
                var transform = parentTransform.TransformMatrix * node.LocalTransform.TransformMatrix;
                
                if(node.IsLeaf) continue;
                foreach (var childNode in node.Children)
                    _traverseQueue.Enqueue(childNode);
            }
        }

        private static void DispatchNode(SceneNode node)
        {
            
        }
    }

}