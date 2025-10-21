#region

using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Core.RenderingSystem.Batching;
using ConcreteEngine.Core.Scene.Entities;
using Tools.DebugInterface;

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
    EntityStore<MeshComponent> Meshes { get; }
    EntityStore<Transform2D> Transforms2D { get; }
    EntityStore<SpriteComponent> Sprites { get; }

    EntityEnumerator<T1> Query<T1>() where T1 : unmanaged;
    EntityEnumerator<T1, T2> Query<T1, T2>() where T1 : unmanaged where T2 : unmanaged;
    EntityEnumerator<T1, T2, T3> Query<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged;
}

public sealed class World : IWorld
{
    public RenderSceneProps RenderProps { get; }

    public WorldSkybox Sky { get; }
    public WorldTerrain Terrain { get; }

    public EntityStore<Transform> Transforms { get; }
    public EntityStore<MeshComponent> Meshes { get; }
    public EntityStore<Transform2D> Transforms2D { get; }
    public EntityStore<SpriteComponent> Sprites { get; }
    


    private int _idIdx = 1;

    internal World(RenderSceneProps renderProps, BatcherRegistry batchers)
    {
        RenderProps = renderProps;
        Terrain = new WorldTerrain(batchers.Get<TerrainBatcher>());
        Sky = new WorldSkybox();

        Transforms2D = GenericStores<Transform2D>.CreateStore();
        Transforms = GenericStores<Transform>.CreateStore();
        Meshes = GenericStores<MeshComponent>.CreateStore();
        Sprites = GenericStores<SpriteComponent>.CreateStore();
        

    }
    public EntityId Create() => new(_idIdx++);
    
    [DebugWatch]
    public int EntityCount => _idIdx;

    [DebugWatch]
    public int ShadowMapSize => RenderProps.Snapshot.Shadows.ShadowMapSize;

    public void Cleanup()
    {
        Transforms.Cleanup();
        Transforms2D.Cleanup();
        Sprites.Cleanup();
        Meshes.Cleanup();
    }

    public EntityEnumerator<T1> Query<T1>() where T1 : unmanaged =>
        new(GenericStores<T1>.Store);

    public EntityEnumerator<T1, T2> Query<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        => new(GenericStores<T1>.Store, GenericStores<T2>.Store);

    public EntityEnumerator<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        => new(GenericStores<T1>.Store, GenericStores<T2>.Store, GenericStores<T3>.Store);


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