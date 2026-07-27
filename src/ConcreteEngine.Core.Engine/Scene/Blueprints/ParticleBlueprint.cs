using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Particles;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class ParticleBlueprint : RenderBlueprint
{
    public readonly ParticleEmitter Emitter;
    public Material ParticleMaterial => GetMaterial(0);

    public ParticleBlueprint(ParticleEmitter emitter, Material? material) : base(1)
    {
        if (material == null) material = AssetStore.Core.FallbackMaterial;
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
        var matId = ParticleMaterial.MaterialId;
        var policy = new DrawPolicy(DrawQueue.Particles, PassMask.Main);
        var source = new RenderSource(default, matId, 0, EntitySourceKind.Particle);

        var entity = Ecs.RenderCore.AddEntity(source, policy);
        var particle = new ParticleComponent(Emitter.Id);
        Ecs.GetRenderStore<ParticleComponent>().Add(entity, in particle);
        Ecs.SceneLink.BindSceneHandle(entity, Owner.Id);
        RenderEntityIds.Add(entity);
    }

    protected override void OnCommit()
    {
        if (RenderEntityIds.Count == 0) return;
        var entity = RenderEntityIds[0];
        Ecs.RenderCore.GetSource(entity).Mesh = Emitter.BoundMesh;
        Ecs.RenderCore.GetDrawPolicy(entity) = new DrawPolicy(DrawQueue.Particles, PassMask.Main);

    }

    internal override void ApplyTransform(in Matrix4x4 rootMatrix)
    {
        if (RenderEntityIds.Count == 0) return;
        var entity = RenderEntityIds[0];
        
        BoundingBox.GetWorldBounds(in Emitter.LocalBounds(), in rootMatrix, out WorldBounds);
        Ecs.RenderCore.GetModelMatrix(entity) = rootMatrix;
        Ecs.RenderCore.GetWorldBounds(entity) = WorldBounds;
    }

    public void OnAssetChanged(AssetObject asset) { }

    public void OnAssetRemoved(AssetObject asset) { }
}