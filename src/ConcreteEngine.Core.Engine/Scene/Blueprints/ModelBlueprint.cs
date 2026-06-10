using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;

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
    public readonly bool IsAnimated;

    private BoundingBox _worldBounds;
    
    public ModelInstance(SceneObject owner, ModelBlueprint blueprint) : base(owner)
    {
        Blueprint = blueprint;
        IsAnimated = blueprint.GetModel().Animation is not null;
    }

    public override SceneObjectBlueprint GetBlueprint() => Blueprint;
    public ref readonly Transform LocalTransform => ref Blueprint.LocalTransform;
    public ref readonly BoundingBox WorldBounds => ref _worldBounds;

    public int MaterialCount => Blueprint.MaterialCount;

    public Model Model => Blueprint.GetModel();

    internal override void OnCreate()
    {
        var meshes = Model.Meshes;
        for (int i = 0; i < meshes.Length; i++)
        {
            var mesh = meshes[i];
            var material = Blueprint.GetMaterial(i);

            var source = new SourceComponent(
                mesh.MeshId, 
                material.MaterialId, 
                mesh.Info.MeshIndex, 
                EntitySourceKind.Model,
                material.State.DrawQueue, 
                material.State.PassMasks);

            var entity = Ecs.RenderCore.AddEntity(source, in LocalTransform);
            RenderEntityIds.Add(entity);
        }

        if (Model.Animation is { } animation)
            AddAnimationEntities(animation);
    }

    private void AddAnimationEntities(ModelAnimation animation)
    {
        var clip = animation.Clips[0];
        var renderComponent = new SkinningComponent(animation.AnimationId, instance: 0);
        var gameComponent = new AnimationComponent { Duration = clip.Duration, Speed = clip.TicksPerSecond };

        var existing = false;
        var rootEntity = RenderEntityIds[0];
        foreach (var query in Ecs.GetRenderStore<SkinningComponent>().Query())
        {
            ref readonly var c = ref query.Component;
            if (renderComponent.AnimationId != c.AnimationId || renderComponent.Instance != c.Instance)
                continue;

            existing = true;
            rootEntity = query.Entity;
            break;
        }

        if (!existing)
        {
            Ecs.GetRenderStore<SkinningComponent>().Add(rootEntity, renderComponent);

            var gameEntity = Ecs.GameCore.AddEntity();
            GameEntityIds.Add(gameEntity);
            Ecs.GetGameStore<AnimationComponent>().Add(gameEntity, gameComponent);
            Ecs.GetGameStore<RenderLink>().Add(gameEntity, new RenderLink(rootEntity));
        }

        var skinLinkComponent = new SkinLinkComponent { EntityId = rootEntity };
        foreach (var entity in GetRenderEntities())
        {
            Ecs.GetRenderStore<SkinLinkComponent>().Add(entity, skinLinkComponent);
        }
    }

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

    public void ApplyBounds()
    {
        BoundingBox result = default;
        foreach (var entity in GetRenderEntities())
        {
            var meshIndex = Ecs.RenderCore.GetSource(entity).MeshIndex;
            var local = Model.Meshes[meshIndex].LocalBounds;

            ref var bounds = ref Ecs.RenderCore.GetWorldBounds(entity);
            BoundingBox.GetWorldBounds(in local, in Ecs.RenderCore.GetWorldMatrix(entity), out bounds);
            BoundingBox.Merge(in result, in bounds, out result);
        }

    }
    private void UpdateTransform()
    {
        Owner.Transform.GetTransformMatrix(out var rootMatrix);
        foreach (var entity in GetRenderEntities())
        {
            MatrixMath.CreateModelMatrix(in Ecs.Render.Core.GetLocalTransform(entity), out var worldMatrix);
            MatrixMath.MultiplyAffine(ref worldMatrix, in rootMatrix);

            ref var finalMatrix = ref Ecs.Render.Core.GetWorldMatrix(entity);
            if (IsAnimated)
            {
                finalMatrix = worldMatrix;
                continue;
            }


            var meshIndex = Ecs.Render.Core.GetSource(entity).MeshIndex;
            ref readonly var meshMatrix = ref Model.Meshes[meshIndex].WorldTransform;
            MatrixMath.MultiplyAffine(ref finalMatrix, in meshMatrix, in worldMatrix);
        }
    }
}