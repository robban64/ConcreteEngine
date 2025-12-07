#region

using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Store;

#endregion

namespace ConcreteEngine.Editor.Components.State;

internal sealed class CameraState
{
    public ref CameraDataState DataState => ref EditorDataStore.Slot<CameraDataState>.Data;

    public void TriggerWrite()
    {
        EditorDataStore.Slot<CameraDataState>.State.IsDirty = true;
    }

}