#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Graphics;
using ConcreteEngine.Renderer.State;
using ConcreteEngine.Shared.Diagnostics;
using ConcreteEngine.Shared.Rendering;
using EditorData = ConcreteEngine.Editor.Store.EditorDataStore;

#endregion

namespace ConcreteEngine.Engine.Editor;

internal static class EngineDataBridge
{
    private static EntityApiController _entities = null!;
    private static WorldApiController _world = null!;
    private static InteractionController _interactions = null!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        var editorState = EditorDataStore.Input.EditorSelection;
        var action = editorState.Action;

        ProcessCamera();
        ProcessWorldParams();
        ProcessParticle();

        if (editorState.IsDirty && !editorState.Id.IsValid)
        {
            EditorDataStore.State.SelectedId = EditorId.Empty;
            return;
        }

        if (action == EditorMouseAction.None) ProcessEntities();
        else ProcessInteractivity();


        WorldInteractive.SelectedEntityId = new EntityId(editorState.Id);
    }

    public static void WriteFrameMetrics(in RenderFrameInfo frameInfo, in GfxFrameResult frameResult)
    {
        EditorDataStore.MetricState.FrameSample =
            new RenderInfoSample(frameInfo.Fps, frameInfo.Alpha, frameResult.DrawCalls, frameResult.TriangleCount);
        EditorDataStore.MetricState.FrameMetrics = new FrameMetric(frameInfo.FrameIndex, EngineTime.Timestamp, default);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessCamera()
    {
        var camGen = _world.CameraGeneration;

        ref var slot = ref EditorDataStore.Slot<CameraDataState>.SlotState;
        ref var data = ref EditorDataStore.Slot<CameraDataState>.Data;
        if (camGen > slot.Generation || slot.IsRequesting)
            _world.WriteCameraState(out data);
        else if (slot.IsDirty)
            _world.ApplyCameraState(in data);

        slot.Reset(camGen);
    }

    private static void ProcessParticle()
    {
        var slot = EditorDataStore.Slot<ParticleDataState>.SlotState;
        ref var data = ref EditorDataStore.Slot<ParticleDataState>.Data;
        if (data.EmitterHandle == 0) return;
        if (slot.IsDirty)
            _world.ApplyEmitterState(in data);
        else if (slot.IsRequesting)
            _world.WriteEmitterState(ref data);
    }

    public static void ProcessWorldParams()
    {
        var gen = _world.WorldParamGeneration;
        ref var slot = ref EditorDataStore.Slot<WorldParamsData>.SlotState;
        ref var data = ref EditorDataStore.Slot<WorldParamsData>.Data;

        if (slot.RequestInFrames > 0)
        {
            slot.RequestInFrames--;
            if (slot.RequestInFrames == 0) slot.IsRequesting = true;
        }

        if (gen > slot.Generation || slot.IsRequesting)
            _world.WriteWorldRenderParams(out data);
        else if (slot.IsDirty)
            _world.ApplyWorldRenderParams(in data);

        slot.Reset(gen);
    }

    private static void ProcessEntities()
    {
        ref var editorState = ref EditorDataStore.Input.EditorSelection;
        ref var data = ref EditorDataStore.State.EntityState;

        var entityId = new EntityId(editorState.Id);

        if (entityId != data.EntityId || editorState.IsRequesting)
        {
            _entities.WriteSelectedEntity(entityId, ref data);
            EditorDataStore.State.SelectedId = editorState.Id;
            return;
        }

        if (editorState.IsDirty)
        {
            _entities.ApplySelectedEntity(entityId, in data);
            EditorDataStore.State.SelectedId = editorState.Id;
        }
    }

    private static void ProcessInteractivity()
    {
        ref readonly var selection = ref EditorDataStore.Input.EditorSelection;
        ref readonly var mouse = ref EditorDataStore.Input.MouseState;

        if (selection.Action == EditorMouseAction.RaycastSelect)
        {
            var entityId = _interactions.OnClick(mouse.Position, out _, out _);
            if (!entityId.IsValid)
            {
                if (selection.Id > 0) EditorDataStore.State.SelectedId = EditorId.Empty;
                return;
            }

            EditorDataStore.State.SelectedId = AsEditorId(entityId);
        }

        if (selection.Action == EditorMouseAction.RaycastDragTerrain)
        {
            if (!selection.Id.IsValid)
            {
                EditorDataStore.State.SelectedId = EditorId.Empty;
                EditorDataStore.State.EntityState = default;
                return;
            }

            var entityId = new EntityId(selection.Id);
            if (!_interactions.IsDragging)
            {
                var clickEntity = _interactions.OnClick(mouse.Position, out _, out _);
                EditorDataStore.State.SelectedId = AsEditorId(clickEntity);

                if (!clickEntity.IsValid)
                {
                    EditorDataStore.State.EntityState = default;
                    return;
                }

                if (clickEntity != entityId) return;
            }
            else
            {
                EditorDataStore.State.SelectedId = AsEditorId(entityId);
                if (!entityId.IsValid) return;
            }

            _interactions.OnDragEntity(entityId, mouse.Position);
        }
    }
}