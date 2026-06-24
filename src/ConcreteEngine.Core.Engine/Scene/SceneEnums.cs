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
    Name = 1 << 1,
    Visibility = 1 << 2,
    Blueprint = 1 << 3,
    Instance = 1 << 4,
    Transform = 1 << 5,
}