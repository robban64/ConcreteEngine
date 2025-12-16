namespace ConcreteEngine.Engine.Worlds.Entities.Components;

public enum EntitySourceKind : byte
{
    Unknown,
    Model,
    AnimatedModel,
    Particle
}

public enum RenderResolver : byte
{
    None = 0,
    Wireframe = 1,
    Highlight = 2,
    BoundingVolume = 3,
}