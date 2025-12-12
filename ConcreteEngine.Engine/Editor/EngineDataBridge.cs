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
        ProcessCamera();
        ProcessWorldParams();
        ProcessParticle();
        //WorldInteractive.SelectedEntityId = ProcessEntities();
    }

    public static void WriteFrameMetrics(in RenderFrameInfo frameInfo, in GfxFrameResult frameResult)
    {
        /*
        EditorData.MetricState.FrameSample =
            new RenderInfoSample(frameInfo.Fps, frameInfo.Alpha, frameResult.DrawCalls, frameResult.TriangleCount);
        EditorData.MetricState.FrameMetrics = new FrameMetric(frameInfo.FrameIndex, EngineTime.Timestamp, default);
        */
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessCamera()
    {
        var camGen = _world.CameraGeneration;

        ref var slot = ref EditorData.Slot<CameraDataState>.SlotState;
        ref var data = ref EditorData.Slot<CameraDataState>.Data;
        if (camGen > slot.Generation || slot.IsRequesting)
            _world.WriteCameraState(out data);
        else if (slot.IsDirty)
            _world.ApplyCameraState(in data);

        slot.Reset(camGen);
    }

    private static void ProcessParticle()
    {
        var slot = EditorData.Slot<EditorParticleState>.SlotState;
        ref var data = ref EditorData.Slot<EditorParticleState>.Data;
        if (data.EmitterHandle == 0) return;
        if (slot.IsDirty)
            _world.ApplyEmitterState(in data);
        else if (slot.IsRequesting)
            _world.WriteEmitterState(ref data);
    }

    private static void ProcessWorldParams()
    {
        var gen = _world.WorldParamGeneration;
        ref var slot = ref EditorData.Slot<WorldParamsData>.SlotState;
        ref var data = ref EditorData.Slot<WorldParamsData>.Data;

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
/*
    private static EntityId ProcessEntities()
    {
        var input = EditorData.Input.EditorSelection;
        ref var state = ref EditorData.State.EntityState;

        var pendingId = new EntityId(input.Id);
        var activeId = new EntityId(EditorData.State.SelectedEntity);

        if (input.Action != EditorMouseAction.None)
        {
            ProcessInteractivity(input);
            activeId = new EntityId(EditorData.State.SelectedEntity);
            _entities.LoadToEditor(activeId, ref state);
        }
        else if (input.IsDirty && pendingId.IsValid)
        {
            _entities.SaveToEngine(pendingId, in state);
            EditorData.State.SelectedEntity = input.Id;
        }
        else if (pendingId != activeId || input.IsRequesting)
        {
            _entities.LoadToEditor(pendingId, ref state);
            EditorData.State.SelectedEntity = input.Id;
        }

        return new EntityId(EditorData.State.SelectedEntity);
    }

    private static void ProcessInteractivity(EditorSelectionState selection)
    {
        ref readonly var mouse = ref EditorData.Input.MouseState;

        if (selection.Action == EditorMouseAction.RaycastSelect)
        {
            var entityId = _interactions.OnClick(mouse.Position, out _, out _);
            if (!entityId.IsValid)
            {
                if (selection.Id > 0) EditorData.State.SelectedEntity = EditorId.Empty;
                return;
            }

            EditorData.State.SelectedEntity = AsEditorId(entityId);
        }

        if (selection.Action == EditorMouseAction.RaycastDragTerrain)
        {
            if (!selection.Id.IsValid)
            {
                EditorData.State.SelectedEntity = EditorId.Empty;
                EditorData.State.EntityState = default;
                return;
            }

            var entityId = new EntityId(selection.Id);
            if (!_interactions.IsDragging)
            {
                var clickEntity = _interactions.OnClick(mouse.Position, out _, out _);
                EditorData.State.SelectedEntity = AsEditorId(clickEntity);

                if (!clickEntity.IsValid)
                {
                    EditorData.State.EntityState = default;
                    return;
                }

                if (clickEntity != entityId) return;
            }
            else
            {
                EditorData.State.SelectedEntity = AsEditorId(entityId);
                if (!entityId.IsValid) return;
            }

            _interactions.OnDragEntity(entityId, mouse.Position);
        }
    }
    */
}