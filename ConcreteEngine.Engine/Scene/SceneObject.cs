using ConcreteEngine.Engine.Worlds.Entities;

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

    private readonly List<EntityId> _linkedEntities = [];

    internal SceneObject(SceneObjectId id, Guid guid, string name)
    {
        Id = id;
        Guid = guid;
        Name = name;
    }
    
    internal void LinkEntity(EntityId entity) => _linkedEntities.Add(entity);
    internal void LinkEntities(ReadOnlySpan<EntityId> entities) => _linkedEntities.AddRange(entities);
    
    internal void EnsureLinkedCapacity(int capacity) => _linkedEntities.EnsureCapacity(capacity);

}