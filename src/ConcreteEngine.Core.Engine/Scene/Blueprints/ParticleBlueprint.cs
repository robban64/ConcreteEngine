using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class ParticleBlueprint : SceneObjectBlueprint
{
    public required string EmitterName;

    public AssetId Material;

    public required int ParticleCount;

    public required EmitterSpatialParams Definition;
    public required EmitterVisualParams VisualParams;

    public Vector3 Offset = Vector3.Zero;
    public Vector3 Direction = Vector3.UnitY;
    public BoundingBox Bounds = BoundingBox.One;

    public static Transform MakeTransform(ParticleBlueprint bp) => Transform.Identity with { Translation = bp.Offset };
}
public sealed class ParticleInstance : BlueprintInstance
{
    public readonly ParticleBlueprint Blueprint;
    public ParticleEmitter Emitter { get; }
    public override SceneObjectBlueprint GetBlueprint() => Blueprint;

    public ParticleInstance(ParticleBlueprint blueprint, ParticleEmitter emitter)
    {
        Blueprint = blueprint;
        Emitter = emitter;
    }

    protected override void OnCommit(SceneObject sceneObject)
    {
        foreach (var entity in GetRenderEntities())
        {
            ref var source = ref Ecs.Render.Core.GetSource(entity);
            source.Queue = DrawCommandQueue.Particles;
            source.Passes = PassMask.Main;
            source.Mesh = Emitter.BoundMesh;
        }
    }
}