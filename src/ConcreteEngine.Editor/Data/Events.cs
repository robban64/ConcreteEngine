using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using Hexa.NET.ImGuizmo;

namespace ConcreteEngine.Editor.Data;

internal enum EventAction : byte
{
    Unknown,
    Rename,
    Reload,
}

internal abstract class EditorEvent;

internal sealed class SelectionEvent : EditorEvent
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

internal sealed class ToolEvent : EditorEvent
{
    public bool? ShowDebugBounds;
    public bool? GizmoEnabled;
    public ImGuizmoMode GizmoMode = ImGuizmoMode.World;
    public ImGuizmoOperation GizmoOperation;

    public static ToolEvent MakeGizmo(ImGuizmoOperation op) => new()
    {
        GizmoEnabled = true, GizmoMode = ImGuizmoMode.World, GizmoOperation = op
    };

    public static ToolEvent MakeBounds(bool enabled) => new() { ShowDebugBounds = enabled, };
}

internal sealed class ModeEvent : EditorEvent
{
    public bool MetricMode;
}

internal sealed class SceneObjectEvent(EventAction action, SceneObjectId sceneObject, string? name = null)
    : EditorEvent
{
    public readonly EventAction Action = action;
    public readonly SceneObjectId SceneObject = sceneObject;
    public readonly string? Name = name;
}

internal sealed class AssetEvent(EventAction action, AssetId asset, string? name = null)
    : EditorEvent
{
    public readonly EventAction Action = action;
    public readonly AssetId Asset = asset;
    public readonly string? Name = name;
}