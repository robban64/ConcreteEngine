namespace ConcreteEngine.Editor.Definitions;

internal enum EditorViewMode : byte
{
    None,
    Editor,
    Metrics
}

internal enum LeftSidebarMode : byte
{
    Default,
    Assets,
    Entities,
    Scene
}

internal enum RightSidebarMode : byte
{
    Default,
    Property,
    SceneObject,
    Camera,
    World,
    Sky,
    Terrain,
}