#region

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
}