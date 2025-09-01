using System.Numerics;

namespace ConcreteEngine.Core.Scene;

public interface IWorld
{
    GameEntityId Create();
    GameComponentRegistry<Transform2D> Transforms2D { get; }
    GameComponentRegistry<Transform2D> PrevTransforms2D { get; }
    GameComponentRegistry<SpriteComponent> Sprites { get; }
    GameComponentRegistry<TilemapComponent> Tilemaps { get; } 
    GameComponentRegistry<LightComponent> Lights { get; }
}

public sealed class World : IWorld
{
    private int _idIdx = 1;

    public GameEntityId Create() => new (_idIdx++);
    
    public GameComponentRegistry<Transform2D> Transforms2D { get; } = new();
    public GameComponentRegistry<Transform2D> PrevTransforms2D { get; } = new();

    public GameComponentRegistry<SpriteComponent> Sprites { get; } = new();
    public GameComponentRegistry<TilemapComponent> Tilemaps { get; } = new(4);
    public GameComponentRegistry<LightComponent> Lights { get; } = new();
    

    public void Cleanup()
    {
        Transforms2D.Cleanup();
        PrevTransforms2D.Cleanup();
        Sprites.Cleanup();
        Tilemaps.Cleanup();
        Lights.Cleanup();


        foreach (var view in PrevTransforms2D.View2(Transforms2D))
        {
            ref var prev = ref view.Value1;
            ref var curr = ref view.Value2;
            prev.Position = curr.Position;
        }
    }
    
}