using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

[Flags]
internal enum ContextChangeMask : ushort
{
    None = 0,
    Mode = 1 << 0,
    Tool = 1 << 1,
    Selection = 1 << 2,

    ToolSelection = Tool | Selection,
    All = Mode | Tool | Selection
}

internal sealed record EditorContext
{
    public SelectionContext Selection { get; init; }
    public ToolContext Tool { get; init; }
    public ModeContext Mode { get; init; }
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
    public bool HasSceneObject => SelectedSceneId.IsValid();
    public bool HasAsset => SelectedAssetId.IsValid();

    public bool IsEmpty => !SelectedSceneId.IsValid() && !SelectedAssetId.IsValid();
    public bool IsMixed => SelectedSceneId.IsValid() && SelectedAssetId.IsValid();

    public bool HasNewAsset(SelectionContext prev) => HasAsset && prev.SelectedAssetId != SelectedAssetId;

    public bool HasNewScene(SelectionContext prev) => HasSceneObject && prev.SelectedSceneId != SelectedSceneId;

    public bool HasNew(SelectionContext prev, FixedInspectorId id) =>
        prev.FixedInspector != FixedInspector && id == FixedInspector;
}

internal readonly record struct ModeContext(ModeId Id)
{
    public static implicit operator ModeId(ModeContext it) => it.Id;
};