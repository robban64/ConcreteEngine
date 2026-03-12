using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class SceneObjectTemplate
{
    public Guid GId { get; } = Guid.NewGuid();
    public string Name { get; init; }

    public bool Enabled { get; set; } = true;

    public readonly List<SceneObjectBlueprint> Blueprints = [];

    public Transform Transform = Transform.Identity;
    public BoundingBox Bounds = BoundingBox.Identity;

    public SceneObjectTemplate(){}

    public SceneObjectTemplate(string name, in Transform transform)
    {
        Name = name;
        Transform = transform;
    }

    public SceneObjectTemplate(string name, in Transform transform, in BoundingBox bounds) : this(name, in transform)
    {
        Bounds = bounds;
    }

}
