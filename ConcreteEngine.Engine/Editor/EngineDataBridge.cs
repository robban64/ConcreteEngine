using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Shared.Rendering;
using EditorData = ConcreteEngine.Editor.Store.EditorDataStore;

namespace ConcreteEngine.Engine.Editor;

internal static class EngineDataBridge
{
    private static EntityApiController _entities = null!;
    private static WorldApiController _world = null!;
    private static InteractionController _interactions = null!;


    internal static void Attach(EntityApiController entityController, WorldApiController worldController,
        InteractionController interactionController)
    {
        _entities = entityController;
        _world = worldController;
        _interactions = interactionController;
    }

    public static void ProcessEditorDataSlot()
    {
        ProcessCamera();
        ProcessWorldParams();
        ProcessEntities();
    }

    private static void ProcessCamera()
    {
        ref var slot = ref EditorData.Slot<CameraDataState>.State;
        ref var data = ref EditorData.Slot<CameraDataState>.Data;
        if (_world.CameraGeneration > slot.Generation || slot.IsRequesting)
            _world.WriteCameraState(out data);
        else if(slot.IsDirty)
            _world.ApplyCameraState(in data);
        
        slot.Reset(_world.CameraGeneration);
    }

    public static void ProcessWorldParams()
    {
        ref var slot = ref EditorData.Slot<WorldParamsData>.State;
        ref var data = ref EditorData.Slot<WorldParamsData>.Data;
        if (_world.WorldParamGeneration > slot.Generation || slot.IsRequesting)
            _world.WriteWorldRenderParams(out data);
        else if(slot.IsDirty)
            _world.ApplyWorldRenderParams(in data);
        
        slot.Reset(_world.WorldParamGeneration);
    }
    
    public static void ProcessEntities()
    {
        ref var editorState = ref EditorData.Input.EntitySelection;
        ref var data = ref EditorData.StateSlot.SelectedEntityState;
        
        var entityId = new EntityId(editorState.EntityId);

        if (entityId != data.EntityId || editorState.IsRequesting)
        {
            _entities.WriteSelectedEntity(entityId, ref data);
            editorState.IsRequesting = false;
            return;
        }

        if (editorState.IsDirty)
        {
            _entities.ApplySelectedEntity(entityId, in data);
            editorState.IsDirty = false;
            return;
        }
    }
    
    public static void OnEditorClick(in EditorWorldMouseData request, out EditorWorldMouseData response)
    {
        switch (request.Action)
        {
            case EditorMouseAction.SelectEntity:
                response = request;
                response.EntityId = _interactions.OnClick(request.MousePosition, out _, out _);
                break;
            case EditorMouseAction.DragEntityOverTerrain:
                response = request;
                response.EntityId = _interactions.OnDragEntity(request.MousePosition);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}