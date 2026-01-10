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
    Property,
    Camera,
    World,
    Sky,
    Terrain,
}

internal enum ComponentDrawKind : byte
{
    Left,
    Right,
    Both
}

//
internal enum VisualStateSelection: byte
{
    Light,
    Fog,
    Post,
    Shadow
}