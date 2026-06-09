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
    Instance = 1 << 2,
    Transform = 1 << 3,
}
