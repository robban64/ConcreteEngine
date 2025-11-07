namespace ConcreteEngine.Engine.Editor.Definitions;

internal enum EngineCommandScope : byte
{
    None = 0,
    CoreCommand = 1,
    WorldCommand = 2,
    AssetCommand = 3,
    RenderCommand = 4
}

internal enum AssetCommandAction : byte
{
    None = 0,
    ReloadAsset
}

internal enum FboCommandAction : byte
{
    None = 0,
    RecreateScreenDependentFbo = 1,
    RecreateShadowFbo = 2
}

internal enum WorldCommandAction : byte
{
    None,
    Camera,
    EntityModel,
    EntityTransform
}