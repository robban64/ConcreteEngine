using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Transforms;

namespace ConcreteEngine.Core.Scene.Entities;

public readonly record struct GameEntityId(int Id);
public readonly record struct GameEntityInstanceId(int Id);

public interface IGameEntity
{
    GameEntityId Id { get; init; }
    GameEntityInstanceId InstanceId { get; init; }
    GameEntityStatus Status { get; set; }
    
}


public enum GameEntityStatus
{
    Pending,
    Active,
    Disabled,
    Deleted
}



