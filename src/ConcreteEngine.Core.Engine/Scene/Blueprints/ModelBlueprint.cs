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
        IsAnimated = blueprint.Model.Rig is not null;
    }

    public override ModelBlueprint GetBlueprint() => Blueprint;

    public int MaterialCount => Blueprint.MaterialCount;

    public Model Model => Blueprint.Model;

    internal override void OnCreate()
    {
        var meshes = Model.GetMeshes();
        for (int i = 0; i < meshes.Length; i++)
        {
            var mesh = Model.GetMesh(i);
            var material = Blueprint.GetMaterial(i);

            var source = new SourceComponent(
                mesh.MeshId, 
                material.MaterialId, 
                mesh.Info.MeshIndex, 
                EntitySourceKind.Model,
                material.State.DrawQueue, 
                material.State.PassMasks);

            var entity = Ecs.RenderCore.AddEntity(source);
            Ecs.SceneLink.BindSceneHandle(entity, Owner.Id);
            RenderEntityIds.Add(entity);
        }

        if (Model.Rig is { } rig)
        {
            foreach (var entity in GetRenderEntities())
                AnimationManager.Instance.AttachEntity(rig, entity);
        }
    }

    internal override void ApplyTransform(in Matrix4x4 rootMatrix)
    {
        var globalBounds = BoundingBox.Infinite;
        foreach (var entity in GetRenderEntities())
        {
            var meshIndex = Ecs.Render.Core.GetSource(entity).MeshIndex;

            //MatrixMath.CreateModelMatrix(in Ecs.Render.Core.GetLocalTransform(entity), out var worldMatrix);
            //MatrixMath.MultiplyAffine(ref worldMatrix, in rootMatrix);

            ref var finalMatrix = ref Ecs.Render.Core.GetWorldMatrix(entity);
            if (IsAnimated)
                finalMatrix = rootMatrix;
            else
                MatrixMath.MultiplyAffine(ref finalMatrix, in Model.GetMesh(meshIndex).Transform, in rootMatrix);


            ref readonly var localBounds = ref Model.GetMesh(meshIndex).Bounds;
            ref var worldBounds = ref Ecs.RenderCore.GetWorldBounds(entity);
            BoundingBox.GetWorldBounds(in localBounds, in finalMatrix, out worldBounds);
            BoundingBox.Merge(in globalBounds, in worldBounds, out globalBounds);
        }
        WorldBounds = globalBounds;
    }
}