using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;

namespace ConcreteEngine.Core.Engine.Scene;

public abstract class SceneObjectBlueprint
{
    public string DisplayName = string.Empty;
    public Guid GId = Guid.NewGuid();
    public bool IsDirty;
}

public sealed class ModelBlueprint : SceneObjectBlueprint
{
    public AssetId ModelId { get; }

    public Transform LocalTransform = Transform.Identity;

    public readonly MaterialId[] Materials = [];

    public ModelBlueprint(AssetId modelId, params MaterialId[] args)
    {
        ModelId = modelId;
        if (args.Length == 0) return;
        Materials = args;
    }
}

public sealed class ParticleBlueprint : SceneObjectBlueprint
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