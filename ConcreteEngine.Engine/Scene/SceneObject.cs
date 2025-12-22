using ConcreteEngine.Engine.ECS;

namespace ConcreteEngine.Engine.Scene;

public sealed class SceneObject
{
    public SceneObjectId Id { get; }
    public Guid Guid { get; }

    public string Name { get; internal set; }

    public bool Enabled { get; internal set; } = true;

    public bool HasModel { get; internal set; }
    public bool HasAnimation { get; internal set; }
    public bool HasParticle { get; internal set; }

    private readonly List<RenderEntityId> _renderEntities = [];
    private readonly List<GameEntityId> _gameEntities = [];

    internal SceneObject(SceneObjectId id, Guid guid, string name)
    {
        Id = id;
        Guid = guid;
        Name = name;
    }

    internal void AddRenderEntity(RenderEntityId entity) => _renderEntities.Add(entity);
    internal void AddRenderEntities(ReadOnlySpan<RenderEntityId> entities) => _renderEntities.AddRange(entities);

    internal void AddGameEntity(GameEntityId entity) => _gameEntities.Add(entity);
    internal void AddGameEntities(ReadOnlySpan<GameEntityId> entities) => _gameEntities.AddRange(entities);


    internal void EnsureCapacity(int capacity)
    {
        _renderEntities.EnsureCapacity(capacity);
    }
}