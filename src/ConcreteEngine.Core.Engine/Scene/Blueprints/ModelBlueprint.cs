using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;

namespace ConcreteEngine.Core.Engine.Scene;

/*
 
   public sealed class ModelBlueprint : SceneObjectBlueprint
   {
       public AssetId ModelId { get; }
       public readonly AssetId[] Materials = [];
   
       public Transform LocalTransform = Transform.Identity;
   
       public ModelBlueprint(AssetId modelId, params ReadOnlySpan<AssetId> materialAssetIds)
       {
           ModelId = modelId;
           if (materialAssetIds.Length >= 1)
               Materials = materialAssetIds.ToArray();
       }
   }
 */

public sealed class ModelBlueprint : SceneObjectBlueprint<ModelInstance>, IAssetListener
{
    public Transform LocalTransform = Transform.Identity;
    public BoundingBox LocalBounds = BoundingBox.One;

    private readonly AssetRef<Model> _model;
    private readonly AssetRef<Material>?[] _materials;
    
    public ModelBlueprint(Model model)
    {
        _model = new AssetRef<Model>(model, this);
        _materials = new AssetRef<Material>?[model.Info.MaterialCount];
        for (var i = 0; i < _materials.Length; i++)
        {
            _materials[i] = new AssetRef<Material>(_model.Asset.GetMaterial(i), this);
        }
    }
    public ModelBlueprint(Model model, params ReadOnlySpan<Material?> materials)
    {
        _model = new AssetRef<Model>(model, this);
        _materials = new AssetRef<Material>?[int.Max(model.Info.MaterialCount, materials.Length)];
        for (var i = 0; i < materials.Length; i++)
        {
            var material = materials[i] ?? _model.Asset.GetMaterial(i);
            _materials[i] = new AssetRef<Material>(material, this);
        }
    }
    
    public int MaterialCount => _materials.Length;

    public Model GetModel() => _model.Asset;

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
        if (asset is not Material material || (material.DirtyFlags & AssetDirtyFlag.Structure) == 0) return;
        foreach (var instance in GetInstanceSpan())
            instance.ApplyMaterialState(material.State);
    }

    public void OnAssetRemoved(AssetObject asset)
    {
        if (asset is not Material) return;
        foreach (var instance in GetInstanceSpan())
            instance.ApplyMaterialState(Material.FallbackMaterial.State);

    }

}

public sealed class ModelInstance : BlueprintInstance
{
    public readonly ModelBlueprint Blueprint;
    public override SceneObjectBlueprint GetBlueprint() => Blueprint;

    public ModelInstance(SceneObject sceneObject, ModelBlueprint blueprint) : base(sceneObject)
    {
        Blueprint = blueprint;
    }

    public ref readonly Transform LocalTransform => ref Blueprint.LocalTransform;
    public ref readonly BoundingBox LocalBounds => ref Blueprint.LocalBounds;

    public int MaterialCount => Blueprint.MaterialCount;

    public Model GetModel() => Blueprint.GetModel();

    public void ApplyMaterialState(MaterialState material)
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