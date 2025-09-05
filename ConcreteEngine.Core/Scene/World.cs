using System.Numerics;

namespace ConcreteEngine.Core.Scene;

public interface IWorld
{
    SceneRenderGlobals RenderGlobals { get; }
    
    GameEntityId Create();
    GameComponentStore<Transform> Transforms { get; }
    GameComponentStore<MeshComponent> Meshes { get; }
    GameComponentStore<Transform2D> Transforms2D { get; }
    GameComponentStore<Transform2D> PrevTransforms2D { get; }
    GameComponentStore<SpriteComponent> Sprites { get; }
    GameComponentStore<TilemapComponent> Tilemaps { get; } 
    GameComponentStore<LightComponent> Lights { get; }
}

public sealed class World : IWorld
{
    private int _idIdx = 1;
    
    public SceneRenderGlobals RenderGlobals { get; }

    internal World(SceneRenderGlobals  renderGlobals)
    {
        RenderGlobals =  renderGlobals;
    }

    
    public GameEntityId Create() => new (_idIdx++);
    
    public GameComponentStore<Transform> Transforms { get; } = new();
    public GameComponentStore<MeshComponent> Meshes { get; } = new();
    public GameComponentStore<Transform2D> Transforms2D { get; } = new();
    public GameComponentStore<Transform2D> PrevTransforms2D { get; } = new();
    public GameComponentStore<SpriteComponent> Sprites { get; } = new();
    public GameComponentStore<TilemapComponent> Tilemaps { get; } = new(4);
    public GameComponentStore<LightComponent> Lights { get; } = new();


    public void Cleanup()
    {
        Transforms.Cleanup();
        Transforms2D.Cleanup();
        PrevTransforms2D.Cleanup();
        Sprites.Cleanup();
        Tilemaps.Cleanup();
        Lights.Cleanup();
        Meshes.Cleanup();

        if (PrevTransforms2D.Count > 0)
        {
            foreach (var view in PrevTransforms2D.View2(Transforms2D))
            {
                ref var prev = ref view.Value1;
                ref var curr = ref view.Value2;
                prev.Position = curr.Position;
            }
        }

    }
    
}