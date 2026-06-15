using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class ParticleBlueprint : RenderBlueprint
{
    public required string EmitterName;

    public AssetId Material;

    public required int ParticleCount;

    public required EmitterSpatialParams Definition;
    public required EmitterVisualParams VisualParams;

    public ParticleBlueprint() : base(1)
    {
    }

}
public sealed class ParticleInstance : RenderBlueprintInstance, IAssetListener
{
    public readonly ParticleBlueprint Blueprint;
    public ParticleEmitter Emitter { get; }
    public AssetRef<Material> ParticleMaterial { get; set; }
    public override ParticleBlueprint GetBlueprint() => Blueprint;

    public ParticleInstance(SceneObject owner, ParticleBlueprint blueprint, ParticleEmitter emitter) : base(owner)
    {
        Blueprint = blueprint;
        Emitter = emitter;
    }

    internal override void OnCreate()
    {
        var materialId = ParticleMaterial is not null ? ParticleMaterial.Asset.MaterialId  : Material.FallbackMaterial.MaterialId;

        var source = new SourceComponent(default, materialId, 0, EntitySourceKind.Particle,
            DrawCommandQueue.Particles, PassMask.Main);

        var entity = Ecs.RenderCore.AddEntity(source, in Blueprint.LocalTransform);
        var particle = new ParticleComponent(Emitter.Id);
        Ecs.GetRenderStore<ParticleComponent>().Add(entity, in particle);
        Ecs.SceneLink.BindSceneHandle(entity, Owner.Id);
        RenderEntityIds.Add(entity);
    }

    protected override void OnCommit()
    {
        foreach (var entity in GetRenderEntities())
        {
            ref var source = ref Ecs.Render.Core.GetSource(entity);
            source.Queue = DrawCommandQueue.Particles;
            source.Passes = PassMask.Main;
            source.Mesh = Emitter.BoundMesh;
        }
    }

    internal override void ApplyTransform(in Matrix4x4 rootMatrix)
    {
        foreach (var entity in GetRenderEntities())
        {
            MatrixMath.CreateModelMatrix(in Ecs.Render.Core.GetLocalTransform(entity), out var worldMatrix);
            MatrixMath.MultiplyAffine(ref worldMatrix, in rootMatrix);

            Ecs.Render.Core.GetWorldMatrix(entity) = worldMatrix;
            BoundingBox.GetWorldBounds(new BoundingBox(-Vector3.One*10,Vector3.One*10), in worldMatrix, out Ecs.Render.Core.GetWorldBounds(entity));
        }

    }

    public void OnAssetChanged(AssetObject asset) {}

    public void OnAssetRemoved(AssetObject asset) { }
}