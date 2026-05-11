namespace ConcreteEngine.Editor.Data;

internal enum ModeId : byte
{
    Asset, Scene, Metric
}

internal enum WindowId : byte
{
    Left, Right, Bottom
}

internal enum StateEnums : byte
{
    None,
    Console,
    AssetList,
    AssetInspector,
    SceneList,
    SceneInspector,
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