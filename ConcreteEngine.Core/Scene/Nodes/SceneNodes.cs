namespace ConcreteEngine.Core.Scene.Nodes;

public sealed class SceneNodes
{
    private readonly SceneHierarchy _hierarchy = new();
    private int _nodeIdx = 1;

    private readonly List<NodeCreate> _pendingCreate = [];
    private readonly List<SceneNode> _pendingRemove = [];
    private readonly List<NodeMutation> _pendingMutations = [];
    private readonly HashSet<SceneNode> _removed = [];
    
    private readonly SceneNodeCollector _collector = new();
    
    private bool _isFirstFrame = true;
    
    internal SceneNodes()
    {
    }

    public SceneNode CreateEmptyNode(string name, SceneNode? parent = null) 
    {
        ArgumentNullException.ThrowIfNull(name);
        
        var node = new SceneNode();
        var behaviour = NothingBehaviour.Instance;
        var create = new NodeCreate(node, name, behaviour, parent);
        if(_isFirstFrame)
            ApplyCreate(create);
        else
            _pendingCreate.Add(create);
        
        return node;
    }
    
    public SceneNode CreateNode<TBehaviour>(string name, SceneNode? parent = null, Action<TBehaviour>? initHandler = null) 
        where TBehaviour : class, INodeBehaviour, new()
    {
        ArgumentNullException.ThrowIfNull(name);
        
        var node = new SceneNode();
        var behaviour = new TBehaviour();
        initHandler?.Invoke(behaviour);
        var create = new NodeCreate(node, name, behaviour, parent);
        if(_isFirstFrame)
            ApplyCreate(create);
        else
            _pendingCreate.Add(create);
        
        return node;
    }

    public void RemoveNode(SceneNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _pendingRemove.Add(node);
    }
    
    public void SetVisible(SceneNode node, bool visible)
    {
        ArgumentNullException.ThrowIfNull(node);
        _pendingMutations.Add(new NodeMutation(node, NodeMutationKind.ToggleVisible)
        {
            BoolValue = visible
        });
    }

    public void SetEnabled(SceneNode node, bool visible)
    {
        ArgumentNullException.ThrowIfNull(node);
        _pendingMutations.Add(new NodeMutation(node, NodeMutationKind.ToggleEnabled)
        {
            BoolValue = visible
        });
    }

    internal ISceneNodeCollector Collect()
    {
        _collector.Collect(_hierarchy.Roots);
        return _collector;
    }

    internal void ApplyPending()
    {
        _isFirstFrame = false;
        
        // Create new nodes
        if (_pendingCreate.Count > 0)
        {
            foreach (var create in _pendingCreate)
            {
                var newNode = create.Node;
                if (_removed.Contains(newNode))
                    throw new InvalidOperationException($"Cannot apply changes to removed Node {newNode.Name}");

                ApplyCreate(create);
            }

            _pendingCreate.Clear();
        }
        
        // Apply action
        if (_pendingRemove.Count > 0)
        {
            foreach (var node in _pendingRemove)
            {
                if (!_removed.Add(node))
                    throw new InvalidOperationException($"Node {node.Name} is already deleted");

                _hierarchy.RemoveNode(node);
            }

            _pendingRemove.Clear();
        }     

        // Apply mutations / changes stuff
        if (_pendingMutations.Count > 0)
        {
            foreach (var mutation in _pendingMutations)
            {
                if (_removed.Contains(mutation.Node))
                    throw new InvalidOperationException($"Cannot apply changes to removed Node {mutation.Node.Name}");

                ApplyMutation(mutation);
            }

            _pendingMutations.Clear();
        }
        
        if (_removed.Count > 0)
            _removed.Clear();
    }

    private void ApplyCreate(NodeCreate create)
    {
        var newNode = create.Node;
        newNode.Initialize(create.Name, _nodeIdx++, create.Behaviour);
        if(newNode.Parent == null)
            _hierarchy.AddRoot(newNode);
        else
            _hierarchy.AddChild(newNode.Parent, newNode);
        
    }


    private void ApplyMutation(NodeMutation mutation)
    {
        var node = mutation.Node;
        switch (mutation.Kind)
        {
            case NodeMutationKind.ToggleVisible:
                node.Visible = mutation.BoolValue;
                break;
            case NodeMutationKind.ToggleEnabled:
                node.Enabled = mutation.BoolValue;
                break;
        }
    }
    
    private readonly record struct NodeCreate(SceneNode Node, string Name, INodeBehaviour Behaviour, SceneNode? Parent);

    private enum NodeMutationKind
    {
        ToggleVisible,
        ToggleEnabled,
    }

    private readonly record struct NodeMutation(SceneNode Node, NodeMutationKind Kind)
    {
        private readonly bool? _boolValue = false;

        public bool BoolValue
        {
            get => _boolValue ?? throw new InvalidOperationException("Missing Bool Value");
            init => _boolValue = value;
        }
    }
}