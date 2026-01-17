namespace ConcreteEngine.Editor.Definitions;

internal enum ViewMode : byte
{
    None,
    Cli,
    Main,
}

internal enum LeftSidebarMode : byte
{
    Default,
    Metrics,
    Assets,
    Scene
}

internal enum RightSidebarMode : byte
{
    Default,
    Metrics,
    Visuals,
    World,
    AssetProperty,
    SceneProperty,
}

internal enum ComponentDrawKind : byte
{
    Left,
    Right,
    Both
}

//
internal enum VisualStateKind : byte
{
    Light,
    Fog,
    Post,
    Shadow
}

internal enum WorldSelection : byte
{
    Camera,
    Sky
}