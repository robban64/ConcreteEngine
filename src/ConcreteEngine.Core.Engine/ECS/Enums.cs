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
    Culled = 1 << 0,
    ForceHidden = 1 << 1 
}