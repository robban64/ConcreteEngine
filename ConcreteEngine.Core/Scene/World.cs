#region

using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Scene.Entities;

#endregion

namespace ConcreteEngine.Core.Scene;

public interface IWorld
{
    SceneRenderProperties SceneRenderProps { get; }

    EntityId Create();
    EntityStore<Transform> Transforms { get; }
    EntityStore<MeshComponent> Meshes { get; }
    EntityStore<Transform2D> Transforms2D { get; }
    EntityStore<Transform2D> PrevTransforms2D { get; }
    EntityStore<SpriteComponent> Sprites { get; }
    EntityStore<TilemapComponent> Tilemaps { get; }
    EntityStore<LightComponent> Lights { get; }
}

public sealed class World : IWorld
{
    private int _idIdx = 1;

    public SceneRenderProperties SceneRenderProps { get; }

    internal World(SceneRenderProperties sceneRenderProps)
    {
        SceneRenderProps = sceneRenderProps;
    }


    public EntityId Create() => new(_idIdx++);

    public EntityStore<Transform> Transforms { get; } = new();
    public EntityStore<MeshComponent> Meshes { get; } = new();
    public EntityStore<Transform2D> Transforms2D { get; } = new();
    public EntityStore<Transform2D> PrevTransforms2D { get; } = new();
    public EntityStore<SpriteComponent> Sprites { get; } = new();
    public EntityStore<TilemapComponent> Tilemaps { get; } = new(4);
    public EntityStore<LightComponent> Lights { get; } = new();


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