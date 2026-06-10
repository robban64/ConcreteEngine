using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
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

    public Vector3 Offset = Vector3.Zero;
    public Vector3 Direction = Vector3.UnitY;

    public ParticleBlueprint() : base(1)
    {
    }

    public static Transform MakeTransform(ParticleBlueprint bp) => Transform.Identity with { Translation = bp.Offset };
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

    internal override void ApplyTransform()
    {
        
    }

    public void OnAssetChanged(AssetObject asset) {}

    public void OnAssetRemoved(AssetObject asset) { }
}