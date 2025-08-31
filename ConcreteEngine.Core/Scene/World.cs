using System.Numerics;

namespace ConcreteEngine.Core.Scene;

public interface IWorld
{
    GameEntityId Create();
    GameEntityRegistry<Transform2D> Transforms2D { get; }
    GameEntityRegistry<SpriteEntity> Sprites { get; }
    GameEntityRegistry<TilemapEntity> Tilemaps { get; } 
    GameEntityRegistry<LightEntity> Lights { get; }
}

public sealed class World : IWorld
{
    private int _idIdx = 1;

    public GameEntityId Create() => new (_idIdx++);
    
    public GameEntityRegistry<Transform2D> Transforms2D { get; } = new();
    public GameEntityRegistry<SpriteEntity> Sprites { get; } = new();
    public GameEntityRegistry<TilemapEntity> Tilemaps { get; } = new(4);
    public GameEntityRegistry<LightEntity> Lights { get; } = new();
    
}