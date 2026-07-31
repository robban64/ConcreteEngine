namespace ConcreteEngine.Core.Engine.RenderEntity;

public enum EntitySourceKind : byte
{
    Model = 0,
    AnimatedModel = 1,
    Particle = 2,
    Foliage = 3
}

[Flags]
public enum DrawEntityFlags : byte
{
    None = 0,
    Skinned = 1 << 0,
    Instanced = 1 << 1,
    Skip = 1 << 2
}


public enum EntityStatus : byte
{
    Unset = 0,
    ForceHidden = 1,
    Normal = 2,
    AlwaysVisible = 3,
}