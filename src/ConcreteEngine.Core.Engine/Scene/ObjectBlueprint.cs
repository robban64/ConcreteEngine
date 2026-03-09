using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class SceneObjectBlueprint
{
    public Guid GId { get; } = Guid.NewGuid();
    public required string Name { get; init; }

    public bool Enabled { get; set; } = true;

    public readonly List<ComponentBlueprint> Components = [];

    public Transform Transform = Transform.Identity;
    public BoundingBox Bounds = BoundingBox.Identity;
}

public abstract class ComponentBlueprint
{
    public string DisplayName = string.Empty;
    public Guid GId = Guid.NewGuid();
    public bool IsDirty;
}

public sealed class ModelBlueprint : ComponentBlueprint
{
    public AssetId ModelId { get; }

    public Transform LocalTransform = Transform.Identity;

    public readonly MaterialId[] MeshIndexToMaterial = [];

    public ModelBlueprint(AssetId modelId, params MaterialId[] args)
    {
        ModelId = modelId;
        if (args.Length == 0) return;
        MeshIndexToMaterial = args;
    }
}

public sealed class ParticleBlueprint : ComponentBlueprint
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