namespace ConcreteEngine.Editor.Data;

internal enum ModeId: byte
{
    Asset, Scene, Metric
}
internal enum WindowId : byte
{
    Left, Right, Bottom
}

internal enum PanelId : byte
{
    None,
    Console,
    MetricsLeft,
    MetricsRight,
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
    Atmosphere,
    Lighting,
    Visual
}