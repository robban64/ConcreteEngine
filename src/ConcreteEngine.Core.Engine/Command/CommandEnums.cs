namespace ConcreteEngine.Core.Engine.Command;

public enum CommandScope : byte
{
    None = 0,
    Core = 1,
    Asset = 2,
    Scene = 3,
    Render = 4
}

public enum CommandAssetAction : byte
{
    None = 0,
    Reload = 1,
}