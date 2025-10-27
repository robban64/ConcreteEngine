#region

using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Core.RenderingSystem.Batching;
using ConcreteEngine.Core.Scene.Entities;
using Core.DebugTools.Data;

#endregion

namespace ConcreteEngine.Core.Scene;

public interface IWorld
{
    public int EntityCount { get; }

    RenderSceneProps RenderProps { get; }
    WorldSkybox Sky { get; }
    WorldTerrain Terrain { get; }

    EntityId Create();
    EntityStore<Transform> Transforms { get; }
    EntityStore<ModelComponent> Meshes { get; }
    EntityStore<Transform2D> Transforms2D { get; }
    EntityStore<SpriteComponent> Sprites { get; }

    EntityEnumerator<T1> Query<T1>() where T1 : unmanaged;
    EntityEnumerator<T1, T2> Query<T1, T2>() where T1 : unmanaged where T2 : unmanaged;
    EntityEnumerator<T1, T2, T3> Query<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged;
}

public sealed class World : IWorld
{
    public RenderSceneProps RenderProps { get; }
    
    public IModelRegistry ModelRegistry {get; private set;}

    public WorldSkybox Sky { get; }
    public WorldTerrain Terrain { get; }
    
    public EntityId Create() => new(_idIdx++);

    private int _idIdx = 1;
    public int EntityCount => _idIdx;
    public int ShadowMapSize => RenderProps.Snapshot.Shadows.ShadowMapSize;


    internal World(RenderSceneProps renderProps, BatcherRegistry batchers)
    {
        RenderProps = renderProps;
        Terrain = new WorldTerrain(batchers.Get<TerrainBatcher>());
        Sky = new WorldSkybox();

        Transforms2D = GenericStores<Transform2D>.CreateStore();
        Transforms = GenericStores<Transform>.CreateStore();
        Meshes = GenericStores<ModelComponent>.CreateStore();
        Sprites = GenericStores<SpriteComponent>.CreateStore();
    }

    internal void AttachModelRegistry(IModelRegistry modelRegistry)
    {
        ModelRegistry = modelRegistry;
        Terrain.AttachModelRegistry(modelRegistry);
        Sky.AttachModelRegistry(modelRegistry);
    }


    public void Cleanup()
    {
        Transforms.Cleanup();
        Transforms2D.Cleanup();
        Sprites.Cleanup();
        Meshes.Cleanup();
    }
    
    public EntityStore<Transform> Transforms { get; }
    public EntityStore<ModelComponent> Meshes { get; }
    public EntityStore<Transform2D> Transforms2D { get; }
    public EntityStore<SpriteComponent> Sprites { get; }


    public EntityEnumerator<T1> Query<T1>() where T1 : unmanaged => new(GenericStores<T1>.Store);

    public EntityEnumerator<T1, T2> Query<T1, T2>() where T1 : unmanaged where T2 : unmanaged =>
        new(GenericStores<T1>.Store, GenericStores<T2>.Store);

    public EntityEnumerator<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged =>
        new(GenericStores<T1>.Store, GenericStores<T2>.Store, GenericStores<T3>.Store);


    private static class GenericStores<T> where T : unmanaged
    {
        public static EntityStore<T> Store { get; private set; } = null!;

        public static EntityStore<T> CreateStore()
        {
            var store = new EntityStore<T>();
            Store = store;
            return store;
        }
    }
}