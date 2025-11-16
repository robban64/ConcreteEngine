using ConcreteEngine.Engine.Worlds.Entities;

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldEntities
{
    public EntityId Create() => new(_idIdx++);
    private int _idIdx = 1;

    public EntityStore<Transform> Transforms { get; }
    public EntityStore<ModelComponent> Meshes { get; }

    internal WorldEntities()
    {
        Transforms = GenericStores<Transform>.CreateStore();
        Meshes = GenericStores<ModelComponent>.CreateStore();
    }
    
    public int EntityCount => _idIdx;

    
    internal void EndTick()
    {
        Transforms.EndTick();
        Meshes.EndTick();
        
        //Transforms2D.EndTick();
        //Sprites.EndTick();
    }


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