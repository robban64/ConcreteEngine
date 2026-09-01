using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.ECS.Render.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics.Animations;

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
            var mat = Blueprint.GetMaterial(i);
            var policy = new DrawPolicy(mat.State.DrawQueue, mat.State.Passes);
            var source = new DrawSource(mesh.MeshId, mat.MaterialId, mesh.Info.MeshIndex);

            var entity = RenderEcs.Core.AddEntity(source, policy);
            SceneManager.Instance.BindSceneHandle(Owner.Id, entity);
            RenderEntityIds.Add(entity);
        }

        if (Model.Rig is { } rig)
        {
            foreach (var entity in GetRenderEntities())
            {
                RenderEcs.Core.ToggleDrawFlag(entity, EntityDrawFlags.Skinned, true);
                AnimationManager.Instance.AttachEntity(rig, entity);
            }

            RenderEcs.Store<SkinningLink>().Commit();
        }
    }

    internal override void ApplyTransform(in Matrix4x4 rootMatrix)
    {
        var globalBounds = BoundingBox.Infinite;
        foreach (var entity in GetRenderEntities())
        {
            var ctx = RenderEcs.Core.GetEntityContext(entity);
            var meshIndex = ctx.Source.MeshIndex;

            //MatrixMath.CreateModelMatrix(in Ecs.Render.Core.GetLocalTransform(entity), out var worldMatrix);
            //MatrixMath.MultiplyAffine(ref worldMatrix, in rootMatrix);

            ref var transform = ref ctx.Transform;

            if (IsAnimated)
                transform.Model = rootMatrix;
            else
            {
                var mesh = Model.GetMesh(meshIndex);
                MatrixMath.MultiplyAffine(ref transform.Model, in mesh.Transform, in rootMatrix);
            }

            MatrixMath.CreateNormalMatrix(ref transform.Normal, in transform.Model);

            ref readonly var localBounds = ref Model.GetMesh(meshIndex).Bounds;
            BoundingAxisBox.GetWorldBounds(in localBounds, in transform.Model, out var entityBounds);
            ctx.WorldBounds = entityBounds;
            globalBounds.Expand(in entityBounds);
        }

        WorldBounds = globalBounds.ToAxisBox();
    }
}