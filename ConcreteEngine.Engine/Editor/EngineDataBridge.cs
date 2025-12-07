using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
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
        ProcessInteractivity();
        ProcessCamera();
        ProcessWorldParams();
        ProcessEntities();
    }

    private static void ProcessInteractivity()
    {
        ref readonly var selection = ref EditorData.Input.EditorSelection;
        ref readonly var mouse = ref EditorData.Input.MouseState;

        if (selection.Action == EditorMouseAction.RaycastSelect)
        {
            var entityId = _interactions.OnClick(mouse.Position, out _, out _);
            if (!entityId.IsValid)
            {
                if (selection.Id > 0) EditorData.StateSlot.SelectedId = EditorId.Empty;
                return;
            }
            EditorData.StateSlot.SelectedId = new EditorId(entityId, EditorItemType.Entity);
        }

        if (selection.Action == EditorMouseAction.RaycastDragTerrain)
        {
            var currentId = EditorData.StateSlot.SelectedId;
            if(currentId.IsValid)
                _interactions.OnDragEntity(new EntityId(currentId), mouse.Position);
        }
    }

    private static void ProcessCamera()
    {
        ref var slot = ref EditorData.Slot<CameraDataState>.State;
        ref var data = ref EditorData.Slot<CameraDataState>.Data;
        if (_world.CameraGeneration > slot.Generation || slot.IsRequesting)
            _world.WriteCameraState(out data);
        else if (slot.IsDirty)
            _world.ApplyCameraState(in data);

        slot.Reset(_world.CameraGeneration);
    }

    public static void ProcessWorldParams()
    {
        ref var slot = ref EditorData.Slot<WorldParamsData>.State;
        ref var data = ref EditorData.Slot<WorldParamsData>.Data;
        if (_world.WorldParamGeneration > slot.Generation || slot.IsRequesting)
            _world.WriteWorldRenderParams(out data);
        else if (slot.IsDirty)
            _world.ApplyWorldRenderParams(in data);

        slot.Reset(_world.WorldParamGeneration);
    }

    public static void ProcessEntities()
    {
        ref var editorState = ref EditorData.Input.EditorSelection;
        ref var data = ref EditorData.StateSlot.EntityState;

        var entityId = new EntityId(editorState.Id);

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

}