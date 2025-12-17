namespace ConcreteEngine.Engine.Scene;

public sealed class GameEntity
{
    public GameEntityId Id { get; }
    public Guid Guid  { get; }
    
    public string Name  { get; private set; }
    
    public bool Enabled { get; private set; }
    public bool HasModel { get; private set; }
    public bool HasAnimation { get; private set; }
    public bool HasParticle { get; private set; }
    
    private GameEntityId[] _entities = [];

    public GameEntity(GameEntityId id, Guid guid)
    {
        Id = id;
        Guid = guid;
    }
}