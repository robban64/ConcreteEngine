using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;

namespace ConcreteEngine.Editor;

public static class EngineController
{
    public static IEngineWorldController WorldController = null!;
    public static IEngineInteractionController InteractionController = null!;
    //public static IEngineEntityController EntityController = null!;
    public static IEngineSceneController SceneController = null!;
    public static IEngineAssetController AssetController = null!;
    
    internal static void SelectEntity(SceneObjectId entity)
    {
        if (entity == EditorDataStore.SelectedSceneObj) return;
        if (!entity.IsValid())
        {
            ConsoleGateway.LogPlain("Invalid selected entity");
            return;
        }

        if (EditorDataStore.SelectedSceneObj.IsValid())
            SceneController.Deselect(EditorDataStore.SelectedSceneObj);

        SceneController.Select(entity);
        EditorDataStore.SelectedSceneObj = entity;
        SceneController.FetchTransform(entity, ref EditorDataStore.Slot<TransformStable>.State);

        /*
        var entityObj = ManagedStore.Get<EditorEntityResource>(entity);

        if (entityObj == null)
            throw new InvalidOperationException($"Entity {entity} not found");

        switch (entityObj.ComponentRef.ItemType)
        {
            case EditorItemType.Particle:
                EditorDataStore.EntityState.ComponentRef = entityObj.ComponentRef;
                FetchParticle();
                break;
            case EditorItemType.Animation:
                EditorDataStore.EntityState.ComponentRef = entityObj.ComponentRef;
                FetchAnimation();
                break;
            default: EditorDataStore.EntityState.ComponentRef = 0; break;
        }*/
    }

    internal static void DeSelectEntity()
    {
        var entity = EditorDataStore.SelectedSceneObj;
        if (!entity.IsValid()) return;

        SceneController.Deselect(entity);
        EditorDataStore.Slot<TransformStable>.State = default;
        EditorDataStore.SelectedSceneObj = default;
    }

    internal static void CommitEntity()
    {
        var entity = EditorDataStore.SelectedSceneObj;
        if (!entity.IsValid())
        {
            ConsoleGateway.LogPlain("Invalid selected entity for commit");
            return;
        }

        SceneController.CommitTransform(entity, in EditorDataStore.Slot<TransformStable>.State);
    }

    internal static void RefreshEntity()
    {
        var entity = EditorDataStore.SelectedSceneObj;
        if (!entity.IsValid())
        {
            ConsoleGateway.LogPlain("Invalid selected entity for refresh");
            return;
        }
        SceneController.FetchTransform(entity, ref EditorDataStore.Slot<TransformStable>.State);
    }
/*
    internal static void FetchAnimation()
    {
        var entity = EditorDataStore.SelectedSceneObj;
        if (!entity.IsValid() || EditorDataStore.EntityState.ComponentRef == 0) return;
        EntityController.FetchAnimation(entity, ref EditorDataStore.AnimationState);
    }

    internal static void CommitAnimation()
    {
        var entity = EditorDataStore.SelectedSceneObj;
        if (!entity.IsValid() || EditorDataStore.EntityState.ComponentRef == 0) return;
        EntityController.CommitAnimation(entity, in EditorDataStore.AnimationState);
    }

    internal static void FetchParticle()
    {
        var entity = EditorDataStore.SelectedSceneObj;
        if (!entity.IsValid() || EditorDataStore.EntityState.ComponentRef == 0) return;
        EntityController.FetchParticle(entity, ref EditorDataStore.ParticleState);
    }

    internal static void CommitParticle()
    {
        var entity = EditorDataStore.SelectedSceneObj;
        if (!entity.IsValid() || EditorDataStore.EntityState.ComponentRef == 0) return;
        EntityController.CommitParticle(entity, in EditorDataStore.ParticleState);
    }
*/
    internal static void CommitCamera()
    {
        WorldController.CommitCamera(EditorDataStore.Slot<EditorCameraState>.GetView());
    }

    internal static void FetchCamera()
    {
        WorldController.FetchCamera(EditorDataStore.Slot<EditorCameraState>.GetView());
    }

    internal static void CommitWorldParams()
    {
        WorldController.CommitWorldRenderParams(EditorDataStore.Slot<WorldParamsData>.GetView());
    }

    internal static void FetchWorldParams()
    {
        WorldController.FetchWorldRenderParams(EditorDataStore.Slot<WorldParamsData>.GetView());
    }
}