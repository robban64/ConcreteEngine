using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Particles;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;

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
        var source = new RenderSource(default, matId, flags: EntityDrawFlags.Instanced);
        var entity = RenderEcs.Core.AddEntity(source, policy);
        RenderEcs.Store<EmitterLink>().Add(entity, new EmitterLink(Emitter.Id));
        RenderEcs.Store<DrawInstancedComponent>().Add(entity, new DrawInstancedComponent(Emitter.ParticleCount));

        SceneManager.Instance.BindSceneHandle(entity, Owner.Id);
        RenderEntityIds.Add(entity);
        
        RenderEcs.Store<EmitterLink>().Commit();
        RenderEcs.Store<DrawInstancedComponent>().Commit();

    }

    protected override void OnCommit()
    {
        if (RenderEntityIds.Count == 0) return;
        var entity = RenderEntityIds[0];
        RenderEcs.Core.GetSource(entity).Mesh = Emitter.BoundMesh;
        RenderEcs.Core.GetDrawPolicy(entity) = new DrawPolicy(DrawQueue.Particles, PassMask.Main);
        RenderEcs.Store<DrawInstancedComponent>().Get(entity).Instances = (uint)Emitter.ParticleCount;


    }

    internal override void ApplyTransform(in Matrix4x4 rootMatrix)
    {
        if (RenderEntityIds.Count == 0) return;
        var entity = RenderEntityIds[0];
        
        BoundingAxisBox.GetWorldBounds(in Emitter.LocalBounds(), in rootMatrix, out WorldBounds);
        RenderEcs.Core.GetModelMatrix(entity) = rootMatrix;
        RenderEcs.Core.GetWorldBounds(entity) = WorldBounds;
    }

    public void OnAssetChanged(AssetObject asset) { }

    public void OnAssetRemoved(AssetObject asset) { }
}