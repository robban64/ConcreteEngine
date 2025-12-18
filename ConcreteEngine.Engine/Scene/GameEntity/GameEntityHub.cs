using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Entities.Resources;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Scene.GameEntity;

public sealed class GameEntityHub
{
    private static readonly List<IGameEntityStore> StoreList = [];
    
    public GameEntityEnumerator<T1> Query<T1>() where T1 : unmanaged, IEntityComponent 
        => new(GenericStores<T1>.Store);

    public GameEntityEnumerator<T1, T2> Query<T1, T2>() 
        where T1 : unmanaged, IEntityComponent where T2 : unmanaged, IEntityComponent =>
        new(GenericStores<T1>.Store, GenericStores<T2>.Store);
    
    private static class GenericStores<T> where T : unmanaged, IEntityComponent
    {
        public static GameEntityStore<T> Store = null!;

        public static GameEntityStore<T> CreateStore(int cap)
        {
            var store = new GameEntityStore<T>(cap);
            StoreList.Add(store);
            Store = store;
            return store;
        }
    }
}