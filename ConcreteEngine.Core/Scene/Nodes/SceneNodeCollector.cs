using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Transforms;

namespace ConcreteEngine.Core.Scene.Nodes;

public interface ISceneNodeCollector
{
    IReadOnlyList<SceneNode> GetSceneNodes<T>() where T : INodeBehaviour;
}

public sealed class SceneNodeCollector : ISceneNodeCollector
{
    private readonly Queue<SceneNode> _traverseQueue = new();
    private readonly Dictionary<Type, List<SceneNode>> _nodeRegistry = new();
    
    public IReadOnlyList<SceneNode> GetSceneNodes<T>() where T : INodeBehaviour
    {
        if (!_nodeRegistry.TryGetValue(typeof(T), out var nodes))
            return [];
        
        return nodes;
    }

    public void Collect(IReadOnlyList<SceneNode> roots)
    {
        foreach (var nodes in _nodeRegistry.Values)
        {
            nodes.Clear();
        }
        
        foreach (var root in roots)
        {
            if(root.Enabled) _traverseQueue.Enqueue(root);
        }
                

        while (_traverseQueue.Count > 0)
        {
            var node = _traverseQueue.Dequeue();
            if(!node.Enabled) continue;
            
            node.UpdateWorldTransform();
            
            ProcessNode(node);
            
            if(node.IsLeaf) continue;
            foreach (var childNode in node.Children)
                _traverseQueue.Enqueue(childNode);
        }

    }

    private void ProcessNode(SceneNode node)
    {
        if(!_nodeRegistry.TryGetValue(node.Behaviour.GetType(), out var nodes))
            _nodeRegistry[node.Behaviour.GetType()] = nodes = [];
        
        nodes.Add(node);
    }
    
}