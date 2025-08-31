using ConcreteEngine.Core.Transforms;

namespace ConcreteEngine.Core.Scene.Nodes;

public enum SceneNodeStatus
{
    Pending,
    Alive,
    Destroyed,
}

public sealed class SceneNode
{
    private string _name = null!;
    private SceneNode? _parent;
    private INodeBehaviour _behaviour = null!;

    private readonly List<SceneNode> _children = new();

    public SceneNodeStatus Status { get; internal set; } = SceneNodeStatus.Pending;

    public int Id { get; internal set; } = -1;
    public bool Enabled { get; internal set; } = true;
    public bool Visible { get; internal set; } = true;
    public ModelTransform2D LocalTransform { get; } = new();
    public WorldTransform WorldTransform { get; } = new();

    public SceneNode? Parent
    {
        get => _parent;
        internal set
        {
            _parent?._children.Remove(this);
            _parent = value;
            _parent?._children.Add(this);
        }
    }

    public string Name
    {
        get => _name;
        internal set
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, 16, nameof(Name));
            _name = value;
        }
    }

    public IReadOnlyList<SceneNode> Children => _children;
    public INodeBehaviour Behaviour => _behaviour;

    public bool IsRoot => Parent == null;
    public bool IsLeaf => Children.Count == 0;

    internal SceneNode()
    {
    }

    public void AddChild(SceneNode child)
    {
        _behaviour.ValidateChildNode(child);
        child.Parent = this;
        _children.Add(child);
    }

    public bool RemoveChild(SceneNode child)
    {
        return _children.Remove(child);
    }

    public T GetBehaviour<T>() where T : INodeBehaviour
    {
        if (Behaviour is T t) return t;

        throw new ArgumentException(
            $"Invalid behaviour type expected {Behaviour.GetType().Name} got  {typeof(T).Name}");
    }

    internal void Initialize(string name, int id, INodeBehaviour behaviour)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(behaviour);
        Name = name;
        Id = id;
        _behaviour = behaviour;
        Status = SceneNodeStatus.Alive;
    }

    internal void UpdateWorldTransform()
    {
        WorldTransform.UpdateWorldTransform(LocalTransform, _parent?.WorldTransform);
    }

    internal void Destroy()
    {
        Status = SceneNodeStatus.Destroyed;

        if (_children.Count > 0)
        {
            foreach (var child in _children)
            {
                child.Destroy();
            }

            _children.Clear();
        }

        _behaviour = null;
        _parent = null;
    }
}