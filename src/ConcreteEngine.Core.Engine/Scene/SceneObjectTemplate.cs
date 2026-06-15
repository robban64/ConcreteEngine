using System.Diagnostics.CodeAnalysis;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class SceneObjectTemplate
{
    public Guid GId { get; } = Guid.NewGuid();
    public string Name { get; init; }
    public bool Enabled { get; set; } = true;

    public IBlueprint[] Blueprints = [];

    public Transform Transform = Transform.Identity;

    public SceneObjectTemplate() { }

    public SceneObjectTemplate(string name, in Transform transform)
    {
        Name = name;
        Transform = transform;
    }
}