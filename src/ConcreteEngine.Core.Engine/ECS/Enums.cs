namespace ConcreteEngine.Core.Engine.ECS;

public enum EntitySourceKind : byte
{
    Unknown,
    Model,
    AnimatedModel,
    Particle
}

[Flags]
public enum VisibilityFlags : byte
{
    Visible = 0,
    UserHidden = 1 << 0, // editor
    Culled = 1 << 1,
    ForceHidden = 1 << 2 // script
}