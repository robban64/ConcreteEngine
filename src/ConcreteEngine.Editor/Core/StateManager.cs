using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Resources;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;

namespace ConcreteEngine.Editor.Core;

internal readonly record struct ToolContext(bool ShowDebugBounds, bool GizmoEnabled, ImGuizmoOperation GizmoOp, ImGuizmoMode GizmoMode);

internal readonly record struct SelectionContext(AssetId SelectedAssetId, SceneObjectId SelectedSceneId, FixedInspectorId FixedInspector)
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
        => prev.FixedInspector != FixedInspector && id ==  FixedInspector;

}

internal readonly record struct ModeContext(bool IsMetricMode);

internal sealed record EditorContext
{
    public SelectionContext Selection { get; init; }
    public ToolContext Tool { get; init; }
    public ModeContext Mode { get; init; }
}

internal sealed class StateManager(
    EventManager eventManager,
    SelectionManager selection,
    GfxResourceApi gfxApi)
{
    public Action<EditorContext, EditorContext>? ContextChanged;

    public readonly SelectionManager Selection = selection;
    public EditorContext Context = new() { Mode = new ModeContext { IsMetricMode = false } };


    public void EmitChange(EditorContext context)
    {
        if(Context == context)
        {
            ConsoleGateway.LogPlain("Identical context emitted");
            return;
        }

        var prev = Context;
        Context = context;
        ContextChanged?.Invoke(prev, context);

        if (prev.Selection != context.Selection)
        {
            Selection.SelectionContextChange(context.Selection);
        }
    }

    public void EnqueueEvent<TEvent>(TEvent evt) where TEvent : EditorEvent => eventManager.Enqueue(evt);

    public bool TryGetTextureRefPtr(TextureId id, out ImTextureRefPtr refPtr)
    {
        if (!id.IsValid())
        {
            refPtr = default;
            return false;
        }

        refPtr = ImGui.ImTextureRef(new ImTextureID(gfxApi.GetNativeHandle(id)));
        return true;
    }
}