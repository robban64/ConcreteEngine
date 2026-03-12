using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer;
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

    private static int _nameTick = 1;

    internal SceneManager(AssetSystem assetSystem, World world)
    {
        _world = world;
        _assetStore = assetSystem.Store;
        _materialStore = assetSystem.MaterialStore;
        _store = new SceneStore(new BlueprintFactory(world, _assetStore, _materialStore));
    }


    public SceneObject CreateSceneObject(SceneObjectTemplate template) => _store.Create(template);

    public SceneObject SpawnFrom(Model model, in Transform transform)
    {
        var materialIndices = model.AssetRefs.MaterialIndices;
        var materials = materialIndices.Length > 0
            ? new MaterialId[materialIndices.Length]
            : [new MaterialId(1)];

        if (materials.Length > 1)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                _assetStore.TryGetByGuid<Material>(materialIndices[i].AssetGId, out var material);
                materials[i] = material.MaterialId;
            }
        }

        var name = $"{model.Name}-{_nameTick++}";
        return CreateSceneObject(new SceneObjectTemplate(name, in transform)
        {
            Blueprints = { new ModelBlueprint(model.Id, materials) }
        });
    }

    public SceneObject SpawnFrom(Model model, in Transform transform, params ReadOnlySpan<AssetId> materialIds)
    {
        var materials = new MaterialId[materialIds.Length];
        for (int i = 0; i < materialIds.Length; i++)
        {
            _assetStore.TryGet<Material>(materialIds[i], out var material);
            materials[i] = material.MaterialId;
        }

        var name = $"{model.Name}-{_nameTick++}";
        return CreateSceneObject(new SceneObjectTemplate(name, in transform)
        {
            Blueprints = { new ModelBlueprint(model.Id, materials) }
        });
    }
}