using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor.Data;

internal abstract record EditorEvent;

internal sealed record SelectionEvent : EditorEvent
{
    public bool Clear;
    public readonly AssetId? Asset;
    public readonly SceneObjectId? SceneObject;
    public readonly FixedInspectorId FixedInspector = FixedInspectorId.None;

    private SelectionEvent() { }
    public SelectionEvent(AssetId asset) => Asset = asset;
    public SelectionEvent(SceneObjectId sceneObject) => SceneObject = sceneObject;
    public SelectionEvent(FixedInspectorId fixedInspector) => FixedInspector = fixedInspector;

    public static SelectionEvent MakeClear() => new() { Clear = true };
}

internal sealed record ToolEvent : EditorEvent
{
    public bool? ShowDebugBounds;
    public bool? GizmoEnabled;
    public bool IsWorldGizmo;
    public TransformGizmoOp GizmoOp;

    public static ToolEvent MakeGizmo(TransformGizmoOp op) => new()
    {
        GizmoEnabled = true, IsWorldGizmo = true, GizmoOp = op
    };

    public static ToolEvent MakeBounds(bool enabled) => new() { ShowDebugBounds = enabled, };
}

internal sealed record ModeEvent(ModeId Mode) : EditorEvent;

internal sealed record AssetEvent(AssetId Asset, string? Rename = null, bool Reload = false) : EditorEvent;

internal sealed record SceneObjectEvent(SceneObjectId SceneObject, string? Rename = null)
    : EditorEvent;