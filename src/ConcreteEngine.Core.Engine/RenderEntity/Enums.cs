namespace ConcreteEngine.Core.Engine.RenderEntity;

public enum EntitySourceKind : byte
{
    Unknown,
    Model,
    AnimatedModel,
    Particle
}

public enum EntityStatus : byte
{
    Unset = 0,
    ForceHidden = 1,
    Normal = 2,
    AlwaysVisible = 3,
}