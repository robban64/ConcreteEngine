using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Scene;

public sealed class SceneManager
{
    private readonly World _world;
    private readonly AssetStore _assetStore;
    private readonly MaterialStore _materialStore;
    private readonly SceneStore _store;

    public SceneStore Store => _store;
    public int SceneObjectCount => _store.Count;

    internal SceneManager(AssetSystem assetSystem, World world)
    {
        _world = world;
        _assetStore = assetSystem.Store;
        _materialStore = assetSystem.MaterialStore;
        _store = new SceneStore(new BlueprintFactory(world, _assetStore, _materialStore));
    }


    public SceneObject CreateSceneObject(SceneObjectBlueprint blueprint) => _store.Create(blueprint);
}