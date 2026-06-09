using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class ModelBlueprint : SceneObjectBlueprint
{
    public AssetId ModelId { get; }

    public Transform LocalTransform = Transform.Identity;

    public readonly AssetId[] Materials = [];

    public ModelBlueprint(AssetId modelId, params ReadOnlySpan<AssetId> materialAssetIds)
    {
        ModelId = modelId;
        if(materialAssetIds.Length >= 1)
            Materials = materialAssetIds.ToArray();
    }
}

public sealed class ModelInstance : BlueprintInstance, IAssetListener
{
    public readonly ModelBlueprint Blueprint;

    private readonly AssetRef<Model> _model;
    private readonly AssetRef<Material>?[] _materials;

    public Transform LocalTransform;
    public BoundingBox LocalBounds;

    public override SceneObjectBlueprint GetBlueprint() => Blueprint;

    public ModelInstance(ModelBlueprint blueprint, Model model)
    {
        var materialCount = int.Max(model.Info.MaterialCount, blueprint.Materials.Length);

        Blueprint = blueprint;
        _model = new AssetRef<Model>(model, this);
        _materials = new AssetRef<Material>[materialCount];

        LocalTransform = blueprint.LocalTransform;
        LocalBounds = model.Bounds;
    }

    public Model AssetModel => _model.Asset;
    public int MaterialCount => _materials.Length;

    public Material GetMaterial(int index)
    {
        if ((uint)index >= (uint)_materials.Length) Throwers.InvalidArgument(nameof(index));
        var material = _materials[index];
        return material is null ? Material.FallbackMaterial : material.Asset;
    }

    public void SetMaterial(int index, Material material)
    {
        if (_materials[index] is { } currentMaterial)
        {
            if (currentMaterial.Asset == material) return;
            currentMaterial.Detach();
        }

        _materials[index] = new AssetRef<Material>(material, this);
    }

    public void OnAssetChanged(AssetObject asset)
    {
        if (asset is Material material && (material.DirtyFlags & AssetDirtyFlag.Structure) != 0)
            ApplyMaterialState(material.State);
    }

    public void OnAssetRemoved(AssetObject asset)
    {
        if (asset is not Material material) return;
        ApplyMaterialState(Material.FallbackMaterial.State);
        for (var i = 0; i < _materials.Length; i++)
        {
            if (_materials[i]?.Asset == material) _materials[i] = null;
        }
    }

    private void ApplyMaterialState(MaterialState material)
    {
        foreach (var entity in GetRenderEntities())
        {
            ref var source = ref Ecs.Render.Core.GetSource(entity);
            if (source.Material.Id > 0 && source.Material != material.MaterialId) continue;
            source.Queue = material.DrawQueue;
            source.Passes = material.PassMasks;
        }
    }
}