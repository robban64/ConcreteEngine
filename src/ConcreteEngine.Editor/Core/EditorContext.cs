using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGuizmo;

namespace ConcreteEngine.Editor.Core;

[Flags]
internal enum ContextChangeMask : ushort
{
    None = 0, 
    Mode = 1 << 0, 
    Tool = 1 << 1,
    Selection = 1 << 2,

    ToolSelection = Tool | Selection
}

internal sealed record EditorContext
{
    public SelectionContext Selection { get; init; }
    public ToolContext Tool { get; init; }
    public ModeContext Mode { get; init; }
}

internal readonly record struct ToolContext(
    bool ShowDebugBounds,
    bool GizmoEnabled,
    ImGuizmoOperation GizmoOp,
    ImGuizmoMode GizmoMode);

internal readonly record struct SelectionContext(
    AssetId SelectedAssetId,
    SceneObjectId SelectedSceneId,
    FixedInspectorId FixedInspector)
{
    public bool HasSceneObject => SelectedSceneId.IsValid();
    public bool HasAsset => SelectedAssetId.IsValid();

    public bool IsEmpty => !SelectedSceneId.IsValid() && !SelectedAssetId.IsValid();
    public bool IsMixed => SelectedSceneId.IsValid() && SelectedAssetId.IsValid();

    public bool IsNewAsset(SelectionContext prev)
        => HasAsset && prev.SelectedAssetId != SelectedAssetId;

    public bool IsNewScene(SelectionContext prev)
        => HasSceneObject && prev.SelectedSceneId != SelectedSceneId;

    public bool IsNew(SelectionContext prev, FixedInspectorId id)
        => prev.FixedInspector != FixedInspector && id == FixedInspector;
}

internal readonly record struct ModeContext(bool IsMetricMode);
