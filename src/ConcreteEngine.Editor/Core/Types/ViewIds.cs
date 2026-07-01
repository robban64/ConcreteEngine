namespace ConcreteEngine.Editor.Core;

internal enum WindowId : byte
{
    Left, Right, BottomLeft, Bottom
}

internal enum InspectorId : byte
{
    None,
    Asset,
    SceneObject,
    Camera,
    Lighting,
    Visual
}

internal enum FixedInspectorId : byte
{
    None,
    Camera,
    Lighting,
    Visual
}