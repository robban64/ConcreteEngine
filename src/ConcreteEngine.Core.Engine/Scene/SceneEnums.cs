namespace ConcreteEngine.Core.Engine.Scene;

public enum SceneObjectKind : byte
{
    Empty = 0,
    Model = 1,
    Particle = 2
}

[Flags]
public enum SceneDirtyFlags : byte
{
    None = 0,
    Enabled = 1 << 0,
    Visibility = 1 << 1,
    Blueprint = 1 << 2,
    Instance = 1 << 3,
    Transform = 1 << 4,
}
