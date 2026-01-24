using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;

namespace ConcreteEngine.Core.Engine.Scene;

public interface IComponentBlueprint
{
}

public sealed class SceneObjectBlueprint
{
    public Guid GId { get; } = Guid.NewGuid();
    public required string Name { get; init; }

    public bool Enabled { get; set; } = true;

    public readonly List<IComponentBlueprint> Components = [];

    public Transform Transform = Transform.Identity;
    public BoundingBox Bounds = BoundingBox.Identity;
}

public sealed class ModelBlueprint : IComponentBlueprint
{
    public AssetId ModelId { get; }

    public Transform LocalTransform = Transform.Identity;

    public readonly Dictionary<int, MaterialId> MeshIndexToMaterial = [];

    public ModelBlueprint(AssetId modelId, params ReadOnlySpan<MaterialId> args)
    {
        ModelId = modelId;
        if (args.Length == 0) return;

        for (int i = 0; i < args.Length; i++)
        {
            MeshIndexToMaterial[i] = args[i];
        }
    }
}

public sealed class ParticleBlueprint : IComponentBlueprint
{
    public required string EmitterName;

    public required MaterialId MaterialId;

    public required int ParticleCount;

    public required ParticleDefinition Definition;
    public required ParticleState State;

    public Vector3 Offset = Vector3.Zero;
    public BoundingBox Bounds = new(new Vector3(-1), new Vector3(1));

    public static Transform MakeTransform(ParticleBlueprint bp) => Transform.Identity with { Translation = bp.Offset };
}