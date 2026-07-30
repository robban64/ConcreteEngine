namespace ConcreteEngine.Core.Engine.RenderEntity;

public enum EntitySourceKind : byte
{
    Unknown,
    Model,
    AnimatedModel,
    Particle
}


public enum EntityVisibility : byte
{
    Visible = 0,
    AlwaysVisible = 1,
    ForceHidden = 2,
    Culled = 3,
}