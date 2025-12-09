#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Shared.Rendering;

#endregion

namespace ConcreteEngine.Editor.Components.State;

internal sealed class WorldParamState
{
    public ref WorldParamsData DataState => ref EditorDataStore.Slot<WorldParamsData>.Data;
    public WorldParamSelection Selection { get; set; }

    public void TriggerChange()
    {
        EditorDataStore.Slot<WorldParamsData>.SlotState.IsDirty = true;
    }

    public void SetSelection(WorldParamSelection selection)
    {
        if (selection == Selection) return;
        Selection = selection;
    }

    public void SetShadowSize(int size)
    {
        if(size == DataState.Shadow.ShadowMapSize) return;
        var payload = new EditorShadowCommand(size, true, EditorRequestAction.Set);
        ModelManager.WorldRenderStateContext.TriggerEvent(EventKey.WorldActionInvoke, payload);
        
        EditorDataStore.Slot<WorldParamsData>.SlotState.RequestInFrames = 4;
    }

}