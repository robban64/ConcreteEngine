using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Shared.Rendering;
using EditorData = ConcreteEngine.Editor.Store.EditorDataStore;

namespace ConcreteEngine.Engine.Editor;

internal static class EngineDataBridge
{
    private static EntityApiController _entities = null!;
    private static WorldApiController _world = null!;
    private static InteractionController _interactions = null!;

    private static EditorId AsEditorId(EntityId entity) => new(entity, EditorItemType.Entity);

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
        if (EditorData.Input.EditorSelection.Action == EditorMouseAction.None)
            ProcessEntities();
        else
            ProcessInteractivity();
        WorldInteractive.SelectedEntityId = new EntityId(EditorData.State.SelectedId);
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
                if (selection.Id > 0) EditorData.State.SelectedId = EditorId.Empty;
                return;
            }

            EditorData.State.SelectedId = AsEditorId(entityId);
        }

        if (selection.Action == EditorMouseAction.RaycastDragTerrain)
        {
            if (!selection.Id.IsValid)
            {
                EditorData.State.SelectedId = EditorId.Empty;
                EditorData.State.EntityState = default;
                return;
            }

            var entityId = new EntityId(selection.Id);
            if (!_interactions.IsDragging)
            {
                var clickEntity = _interactions.OnClick(mouse.Position, out _, out _);
                EditorData.State.SelectedId = AsEditorId(clickEntity);

                if (!clickEntity.IsValid)
                {
                    EditorData.State.EntityState = default;
                    return;
                }

                if (clickEntity != entityId) return;
            }
            else
            {
                EditorData.State.SelectedId = AsEditorId(entityId);
                if (!entityId.IsValid) return;
            }

            _interactions.OnDragEntity(entityId, mouse.Position);
        }
    }

    private static void ProcessCamera()
    {
        ref var slot = ref EditorData.Slot<CameraDataState>.SlotState;
        ref var data = ref EditorData.Slot<CameraDataState>.Data;
        if (_world.CameraGeneration > slot.Generation || slot.IsRequesting)
            _world.WriteCameraState(out data);
        else if (slot.IsDirty)
            _world.ApplyCameraState(in data);

        slot.Reset(_world.CameraGeneration);
    }

    public static void ProcessWorldParams()
    {
        ref var slot = ref EditorData.Slot<WorldParamsData>.SlotState;
        ref var data = ref EditorData.Slot<WorldParamsData>.Data;
        if (_world.WorldParamGeneration > slot.Generation || slot.IsRequesting)
            _world.WriteWorldRenderParams(out data);
        else if (slot.IsDirty)
            _world.ApplyWorldRenderParams(in data);

        slot.Reset(_world.WorldParamGeneration);
    }

    private static void ProcessEntities()
    {
        ref var editorState = ref EditorData.Input.EditorSelection;
        ref var data = ref EditorData.State.EntityState;

        var entityId = new EntityId(editorState.Id);

        if (editorState.IsDirty && !editorState.Id.IsValid)
        {
            data = default;
            EditorData.State.SelectedId = EditorId.Empty;
            return;
        }

        if (entityId != data.EntityId || editorState.IsRequesting)
        {
            _entities.WriteSelectedEntity(entityId, ref data);
            EditorData.State.SelectedId = editorState.Id;
            return;
        }

        if (editorState.IsDirty)
        {
            _entities.ApplySelectedEntity(entityId, in data);
            EditorData.State.SelectedId = editorState.Id;
        }
    }
}