using System.Numerics;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Scene.Data;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Objects;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Scene;



public sealed class SceneWorld
{
    public GameEntityStore EntityStore { get; }

    private readonly World _world;
    private readonly AssetStore _assetStore;
    private readonly MaterialStore _materialStore;

    internal SceneWorld(AssetSystem assetSystem, World world)
    {
        GameEntityFactory.Initialize();
        
        EntityStore = new  GameEntityStore(world, assetSystem.StoreImpl, assetSystem.MaterialStoreImpl);
        _assetStore = assetSystem.StoreImpl;
        _materialStore = assetSystem.MaterialStoreImpl;
        _world = world;
    }

    public void CreateEntity(string name, string template)
    {
        
    }
    
}