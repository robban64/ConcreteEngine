using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Particles;
using ConcreteEngine.Core.Engine.Render;
using ConcreteEngine.Core.Engine.Render.Components;

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
        var policy = new DrawPolicy(DrawQueue.Particles, PassMask.Scene);
        var source = new DrawSource(default, matId, DrawMask: EntityDrawMask.Instanced);
        var entity = RenderWorld.Instance.AddEntity(source, policy);
        RenderWorld.Sparse<EmitterLink>().Add(entity, new EmitterLink(Emitter.Id));
        RenderWorld.Sparse<DrawInstanced>().Add(entity, new DrawInstanced(Emitter.ParticleCount));

        SceneManager.Instance.BindSceneHandle(Owner.Id, entity);
        RenderEntityIds.Add(entity);

        RenderWorld.Sparse<EmitterLink>().Commit();
        RenderWorld.Sparse<DrawInstanced>().Commit();
    }

    protected override void OnCommit()
    {
        if (RenderEntityIds.Count == 0) return;
        
        var entity = RenderEntityIds[0];
        
        var ctx = entity.GetContext();
        ctx.Source.Mesh = Emitter.BoundMesh;
        ctx.Policy = new DrawPolicy(DrawQueue.Particles, PassMask.Scene);
        ctx.GetComponent<DrawInstanced>().Instances = (uint)Emitter.ParticleCount;
    }

    internal override void ApplyTransform(in Matrix4x4 rootMatrix)
    {
        if (RenderEntityIds.Count == 0) return;
        var entity = RenderEntityIds[0];

        
        BoundingAxisBox.GetWorldBounds(in Emitter.LocalBounds, in rootMatrix, out var bounds);
        var ctx = entity.GetContext();
        ctx.Transform = rootMatrix;
        ctx.WorldBounds = new WorldBox(in  bounds);
        WorldBounds = bounds;

    }

    public void OnAssetChanged(AssetObject asset) { }

    public void OnAssetRemoved(AssetObject asset) { }
}