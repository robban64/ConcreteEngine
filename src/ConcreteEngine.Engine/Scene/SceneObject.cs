using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.ECS;

namespace ConcreteEngine.Engine.Scene;

public sealed class SceneObject : ISceneObject, IComparable<ISceneObject>
{
    internal static void Bind(SceneStore sceneStore) => _store = sceneStore;
    private static SceneStore _store = null!;

    private readonly SceneObjectId _id;

    public Guid GId { get; }
    public string Name { get; private set; }
    public bool Enabled { get; private set; }

    private readonly List<IComponentBlueprint> _blueprints;

    private readonly List<RenderEntityId> _renderEntities = [];
    private readonly List<GameEntityId> _gameEntities = [];

    private Transform _transform;
    private BoundingBox _bounds;


    internal SceneObject(SceneObjectId id, Guid gId, string name, bool enabled, List<IComponentBlueprint> blueprints,
        in Transform transform, in BoundingBox bounds)
    {
        _id = id;
        GId = gId;
        Name = name;
        Enabled = enabled;
        _blueprints = blueprints;
        _transform = transform;
        _bounds = bounds;
    }

    //
    public SceneObjectId Id => _id;
    public int RenderEntitiesCount => _renderEntities.Count;
    public int GameEntitiesCount => _gameEntities.Count;

    //
    public ref readonly Transform GetTransform() => ref _transform;
    public ref readonly BoundingBox GetBounds() => ref _bounds;

    //
    public Vector3 Translation
    {
        get => _transform.Translation;
        set
        {
            _transform.Translation = value;
            _store.MakeDirty(_id);
        }
    }

    public Vector3 Scale
    {
        get => _transform.Scale;
        set
        {
            _transform.Scale = value;
            _store.MakeDirty(_id);
        }
    }

    public Quaternion Rotation
    {
        get => _transform.Rotation;
        set
        {
            _transform.Rotation = value;
            _store.MakeDirty(_id);
        }
    }

    //
    public void SetTransform(in Transform transform)
    {
        _transform = transform;
        _store.MakeDirty(_id);
    }

    public void SetBounds(in BoundingBox bounds)
    {
        _bounds = bounds;
        _store.MakeDirty(_id);
    }

    public void SetSpatial(in Transform transform, in BoundingBox bounds)
    {
        _transform = transform;
        _bounds = bounds;
        _store.MakeDirty(_id);
    }

    //
    internal ReadOnlySpan<RenderEntityId> GetRenderEntities() => CollectionsMarshal.AsSpan(_renderEntities);
    internal ReadOnlySpan<GameEntityId> GetGameEntities() => CollectionsMarshal.AsSpan(_gameEntities);

    //Temp
    internal ModelBlueprint GetModelBlueprint(int index) => (ModelBlueprint)_blueprints[index];

    //
    public void AddBlueprint(IComponentBlueprint blueprint)
    {
        //   if(_blueprints.Contains(blueprint))
        //       throw new ArgumentException($"The render blueprint '{blueprint}' is already registered.", nameof(blueprint));

        _blueprints.Add(blueprint);
        _store.MakeDirty(_id);
    }

    //
    internal void AddRenderEntity(RenderEntityId entity)
    {
        _renderEntities.Add(entity);
        _store.MakeDirty(_id);
    }

    internal void AddRenderEntities(ReadOnlySpan<RenderEntityId> entities)
    {
        _renderEntities.AddRange(entities);
        _store.MakeDirty(_id);
    }

    internal void AddGameEntity(GameEntityId entity)
    {
        _gameEntities.Add(entity);
        _store.MakeDirty(_id);
    }

    internal void AddGameEntities(ReadOnlySpan<GameEntityId> entities)
    {
        _gameEntities.AddRange(entities);
        _store.MakeDirty(_id);
    }


    internal void EnsureCapacity(int renderEcsCapacity, int gameEcsCapacity)
    {
        _renderEntities.EnsureCapacity(renderEcsCapacity);
        _gameEntities.EnsureCapacity(gameEcsCapacity);
    }

    public int CompareTo(ISceneObject? other) => other is null ? 1 : _id.CompareTo(other.Id);
}