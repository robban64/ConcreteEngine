using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class SceneSpawner
{
    private static int _nameTick = 1;

    private readonly SceneStore _sore;

    internal SceneSpawner(SceneStore sore)
    {
        _sore = sore;
    }

    public SceneObject CreateSceneObject(SceneObjectTemplate template) => _sore.Create(template);

    public SceneObject SpawnFrom(Model model, in Transform transform)
    {
        var materialIndices = model.AssetRefs.MaterialIndices;
        var materials = materialIndices.Length > 0
            ? new AssetId[materialIndices.Length]
            : [MaterialStore.FallbackMaterial.Id];

        if (materials.Length > 1)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                if (!AssetStore.Instance.TryGetByGuid<Material>(materialIndices[i].AssetGId, out var material))
                    material = MaterialStore.FallbackMaterial;

                materials[i] = material.Id;
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
        var materials = new AssetId[materialIds.Length];
        for (int i = 0; i < materialIds.Length; i++)
        {
            if (!AssetStore.Instance.TryGet<Material>(materialIds[i], out var material))
                material = MaterialStore.FallbackMaterial;

            materials[i] = material.Id;
        }

        var name = $"{model.Name}-{_nameTick++}";
        return CreateSceneObject(new SceneObjectTemplate(name, in transform)
        {
            Blueprints = { new ModelBlueprint(model.Id, materials) }
        });
    }
}