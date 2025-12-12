namespace ConcreteEngine.Editor.Definitions;

public enum EditorItemType : byte
{
    None = 0,
    Unspecified = 1,

    // Assets
    Texture,
    Shader,
    Model,
    MaterialTemplate,
    Material,

    // World
    Entity,
    Particle,
    Animation,
    MaterialKey,

    AnimationKey,
    ParticleEmitter,
}