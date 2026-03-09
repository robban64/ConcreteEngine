using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.ECS;

namespace ConcreteEngine.Core.Engine.Scene;

public interface ISceneObjectNotifier
{
    void MarkDirty(SceneObject sceneObject);
    void Rename(SceneObject asset, string newName, Action<string> onSuccess);
}
public sealed class SceneObject : IEquatable<SceneObject>, IComparable<SceneObject>
{
    private ISceneObjectNotifier? _notifier;

    public SceneObjectId Id { get; }
    public Guid GId { get; }

    public string Name
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            PackedName = StringPacker.PackUtf8(value.AsSpan());
        }
    }

    public ulong PackedName { get; private set; }

    public bool Enabled { get; private set; }

    public SceneObjectKind Kind { get; }

    private readonly List<ComponentBlueprint> _blueprints;

    private readonly List<RenderEntityId> _renderEntities = [];
    private readonly List<GameEntityId> _gameEntities = [];

    private Transform _transform;
    private BoundingBox _bounds;

    internal SceneObject(SceneObjectId id, Guid gId, string name, bool enabled, List<ComponentBlueprint> blueprints,
        in Transform transform, in BoundingBox bounds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Id, nameof(id));
        ArgumentOutOfRangeException.ThrowIfEqual(gId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(blueprints);
        ArgumentException.ThrowIfNullOrEmpty(name);

        Id = id;
        GId = gId;
        Name = name;
        Enabled = enabled;
        _blueprints = blueprints;
        _transform = transform;
        _bounds = bounds;

        if (_blueprints.Count > 0)
        {
            var blueprint = _blueprints[0];
            Kind = blueprint switch
            {
                ModelBlueprint => SceneObjectKind.Model,
                ParticleBlueprint => SceneObjectKind.Particle,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    internal void Attach(ISceneObjectNotifier notifier) => _notifier = notifier;
    public void SetName(string newName)
    {
        if (_notifier is not { } notifier) return;
        notifier.Rename(this, newName, (name) => Name = name);
    }


    //
    public int RenderEntitiesCount => _renderEntities.Count;
    public int GameEntitiesCount => _gameEntities.Count;
    
    //
    public Vector3 Translation
    {
        get => _transform.Translation;
        set
        {
            _transform.Translation = value;
            _notifier?.MarkDirty(this);
        }
    }

    public Vector3 Scale
    {
        get => _transform.Scale;
        set
        {
            _transform.Scale = value;
            _notifier?.MarkDirty(this);
        }
    }

    public Quaternion Rotation
    {
        get => _transform.Rotation;
        set
        {
            _transform.Rotation = value;
            _notifier?.MarkDirty(this);
        }
    }

    //
    public ref readonly Transform GetTransform() => ref _transform;
    public ref readonly BoundingBox GetBounds() => ref _bounds;

    public void SetTransform(in Transform transform)
    {
        _transform = transform;
        _notifier?.MarkDirty(this);
    }

    public void SetBounds(in BoundingBox bounds)
    {
        _bounds = bounds;
        _notifier?.MarkDirty(this);
    }

    //
    public ReadOnlySpan<ComponentBlueprint> GetBlueprints() => CollectionsMarshal.AsSpan(_blueprints);

    public ReadOnlySpan<RenderEntityId> GetRenderEntities() => CollectionsMarshal.AsSpan(_renderEntities);
    public ReadOnlySpan<GameEntityId> GetGameEntities() => CollectionsMarshal.AsSpan(_gameEntities);


    //Temp
    internal ModelBlueprint GetModelBlueprint(int index) => (ModelBlueprint)_blueprints[index];

    //
    internal void AddRenderEntity(RenderEntityId entity)
    {
        _renderEntities.Add(entity);
        _notifier?.MarkDirty(this);
    }

    internal void AddRenderEntities(ReadOnlySpan<RenderEntityId> entities)
    {
        _renderEntities.AddRange(entities);
        _notifier?.MarkDirty(this);
    }

    internal void AddGameEntity(GameEntityId entity)
    {
        _gameEntities.Add(entity);
        _notifier?.MarkDirty(this);
    }

    internal void AddGameEntities(ReadOnlySpan<GameEntityId> entities)
    {
        _gameEntities.AddRange(entities);
        _notifier?.MarkDirty(this);
    }

    internal void EnsureCapacity(int renderEcsCapacity, int gameEcsCapacity)
    {
        _renderEntities.EnsureCapacity(renderEcsCapacity);
        _gameEntities.EnsureCapacity(gameEcsCapacity);
    }

    public int CompareTo(SceneObject? other) => other is null ? 1 : Id.CompareTo(other.Id);

    public bool Equals(SceneObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Id.Equals(other.Id) && GId.Equals(other.GId);
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is SceneObject other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, GId);
}