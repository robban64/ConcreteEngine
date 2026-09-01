namespace ConcreteEngine.Editor.Core;

[Flags]
internal enum ContextChangeMask : ushort
{
    None = 0,
    Tool = 1 << 0,
    Selection = 1 << 1,

    ToolSelection = Tool | Selection,
    All = Tool | Selection
}

internal sealed record EditorContext
{
    public SelectionContext Selection { get; init; }
    public ToolContext Tool { get; init; }
}

internal readonly record struct ToolContext(
    bool Enabled,
    bool ShowDebugBounds,
    bool IsWorldGizmo,
    TransformGizmoOp GizmoOp);

internal readonly record struct SelectionContext(
    AssetId SelectedAssetId,
    SceneObjectId SelectedSceneId,
    FixedInspectorId FixedInspector)
{
    public bool HasAsset => SelectedAssetId.IsValid;
    public bool HasSceneObject => SelectedSceneId.IsValid;

    public bool IsMixed => SelectedSceneId.IsValid || SelectedAssetId.IsValid;
    public bool IsEmpty => !SelectedSceneId.IsValid && !SelectedAssetId.IsValid;

    public bool HasSelection() => IsMixed || FixedInspector > 0;

    public bool HasNewAsset(SelectionContext prev) => HasAsset && prev.SelectedAssetId != SelectedAssetId;
    public bool HasNewScene(SelectionContext prev) => HasSceneObject && prev.SelectedSceneId != SelectedSceneId;

    public bool HasNewFixed(SelectionContext prev, FixedInspectorId id) =>
        prev.FixedInspector != FixedInspector && id == FixedInspector;
}