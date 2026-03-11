using ConcreteEngine.Core.Common.Numerics;

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
