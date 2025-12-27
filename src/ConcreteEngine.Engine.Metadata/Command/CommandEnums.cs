namespace ConcreteEngine.Engine.Metadata.Command;

public enum CommandScope : byte
{
    None = 0,
    CoreCommand = 1,
    WorldCommand = 2,
    AssetCommand = 3,
    RenderCommand = 4
}

public enum CommandAssetAction : byte
{
    None = 0,
    Reload = 1,
}

public enum CommandFboAction : byte
{
    None = 0,
    RecreateScreenDependentFbo = 1,
    RecreateShadowFbo = 2
}