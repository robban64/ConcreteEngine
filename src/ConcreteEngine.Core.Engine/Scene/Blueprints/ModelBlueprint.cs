using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class ModelBlueprint : RenderBlueprint
{
    private readonly AssetRef<Model> _model;
    public Model Model => _model.Asset;

    public ModelBlueprint(Model model) : base(model.Info.MaterialCount)
    {
        _model = new AssetRef<Model>(model, this);
        for (var i = 0; i < Materials.Length; i++)
        {
            Materials[i] = new AssetRef<Material>(_model.Asset.GetMaterial(i), this);
        }
    }
    public ModelBlueprint(Model model, params ReadOnlySpan<Material?> materials) 
        : base(int.Max(model.Info.MaterialCount, materials.Length))
    {
        _model = new AssetRef<Model>(model, this);
        for (var i = 0; i < materials.Length; i++)
        {
            var material = materials[i] ?? _model.Asset.GetMaterial(i);
            Materials[i] = new AssetRef<Material>(material, this);
        }
    }
    


}

public sealed class ModelInstance : RenderBlueprintInstance
{
    public readonly ModelBlueprint Blueprint;
    public readonly bool IsAnimated;

    public ModelInstance(SceneObject owner, ModelBlueprint blueprint) : base(owner)
    {
        Blueprint = blueprint;
        IsAnimated = blueprint.Model.Animation is not null;
    }

    public override ModelBlueprint GetBlueprint() => Blueprint;

    public int MaterialCount => Blueprint.MaterialCount;

    public Model Model => Blueprint.Model;

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

            var entity = Ecs.RenderCore.AddEntity(source, in Blueprint.LocalTransform);
            Ecs.SceneLink.BindSceneHandle(entity, Owner.Id);
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
            //GameEntityIds.Add(gameEntity);
            Ecs.GetGameStore<AnimationComponent>().Add(gameEntity, gameComponent);
            Ecs.GetGameStore<RenderLink>().Add(gameEntity, new RenderLink(rootEntity));
        }

        var skinLinkComponent = new SkinLinkComponent { EntityId = rootEntity };
        foreach (var entity in GetRenderEntities())
        {
            Ecs.GetRenderStore<SkinLinkComponent>().Add(entity, skinLinkComponent);
        }
    }

    internal override void ApplyTransform(in Matrix4x4 rootMatrix)
    {
        BoundingBox globalBounds = default;
        var meshes = Model.Meshes;
        foreach (var entity in GetRenderEntities())
        {
            var meshIndex = Ecs.Render.Core.GetSource(entity).MeshIndex;

            MatrixMath.CreateModelMatrix(in Ecs.Render.Core.GetLocalTransform(entity), out var worldMatrix);
            MatrixMath.MultiplyAffine(ref worldMatrix, in rootMatrix);

            ref var finalMatrix = ref Ecs.Render.Core.GetWorldMatrix(entity);
            if (IsAnimated)
                finalMatrix = worldMatrix;
            else
                MatrixMath.MultiplyAffine(ref finalMatrix, in meshes[meshIndex].WorldTransform, in worldMatrix);


            ref readonly var localBounds = ref meshes[meshIndex].LocalBounds;
            ref var worldBounds = ref Ecs.RenderCore.GetWorldBounds(entity);
            BoundingBox.GetWorldBounds(in localBounds, in finalMatrix, out worldBounds);
            BoundingBox.Merge(in globalBounds, in worldBounds, out globalBounds);
        }
        WorldBounds = globalBounds;
    }
}