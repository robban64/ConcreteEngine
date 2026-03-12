using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;

namespace ConcreteEngine.Core.Engine.Scene;

public abstract class BlueprintInstance(SceneObjectBlueprint blueprint)
{
    public readonly SceneObjectBlueprint Blueprint = blueprint;

    public string DisplayName { get; set; } = blueprint.DisplayName;

    public bool IsDirty { get; set; }

    internal readonly List<RenderEntityId> RenderEntityIds = [];
    internal readonly List<GameEntityId> GameEntityIds = [];

    public ReadOnlySpan<RenderEntityId> GetRenderEntities() => CollectionsMarshal.AsSpan(RenderEntityIds);
    public ReadOnlySpan<GameEntityId> GetGameEntities() => CollectionsMarshal.AsSpan(GameEntityIds);
}

public abstract class BlueprintInstance<TBlueprint>(TBlueprint blueprint) : BlueprintInstance(blueprint)
    where TBlueprint : SceneObjectBlueprint
{
    public new TBlueprint Blueprint { get; } = blueprint;
}

public sealed class ModelInstance(ModelBlueprint blueprint, Model asset)
    : BlueprintInstance<ModelBlueprint>(blueprint)
{
    public Model Asset { get; } = asset;
    public readonly List<Material> Materials = new(asset.Meshes.Length);
    public readonly bool IsAnimated = asset.Animation != null;

    public Transform LocalTransform = blueprint.LocalTransform;
}

public sealed class ParticleInstance(ParticleBlueprint blueprint)
    : BlueprintInstance<ParticleBlueprint>(blueprint)
{
}