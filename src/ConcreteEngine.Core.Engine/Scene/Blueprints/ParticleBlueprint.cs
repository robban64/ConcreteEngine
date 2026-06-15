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
    public readonly ParticleEmitter Emitter;
    public Material ParticleMaterial => GetMaterial(0);

    public ParticleBlueprint(ParticleEmitter emitter, Material? material) : base(1)
    {
        if(material == null) material = Material.FallbackMaterial;
        Materials[0] = new AssetRef<Material>(material, this);
        Emitter = emitter;
        DisplayName = emitter.Name;
    }

}
public sealed class ParticleInstance : RenderBlueprintInstance
{
    public readonly ParticleBlueprint Blueprint;
    public ParticleEmitter Emitter => Blueprint.Emitter;
    public Material ParticleMaterial => Blueprint.ParticleMaterial;
    public override ParticleBlueprint GetBlueprint() => Blueprint;

    public ParticleInstance(SceneObject owner, ParticleBlueprint blueprint) : base(owner)
    {
        Blueprint = blueprint;
    }

    internal override void OnCreate()
    {
        var materialId = ParticleMaterial.MaterialId;
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
        if (RenderEntityIds.Count == 0) return;
        var entity = RenderEntityIds[0];
        
        ref var source = ref Ecs.Render.Core.GetSource(entity);
        source.Queue = DrawCommandQueue.Particles;
        source.Passes = PassMask.Main;
        source.Mesh = Emitter.BoundMesh;
    }

    internal override void ApplyTransform(in Matrix4x4 rootMatrix)
    {
        if (RenderEntityIds.Count == 0) return;
        var entity = RenderEntityIds[0];
        
        Ecs.Render.Core.GetWorldMatrix(entity) = rootMatrix;
        BoundingBox.GetWorldBounds(in Emitter.LocalBounds(), in rootMatrix, out WorldBounds);
        Ecs.Render.Core.GetWorldBounds(entity) = WorldBounds;

    }

    public void OnAssetChanged(AssetObject asset) {}

    public void OnAssetRemoved(AssetObject asset) { }
}