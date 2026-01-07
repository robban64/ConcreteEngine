using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
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
    public static IEngineEntityController EntityController = null!;
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
            EntityController.DeselectEntity(EditorDataStore.SelectedSceneObj);

        EntityController.SelectEntity(entity, ref EditorDataStore.EntityState);
        EditorDataStore.SelectedSceneObj = entity;

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
        }
    }

    internal static void DeSelectEntity()
    {
        var entity = EditorDataStore.SelectedSceneObj;
        if (!entity.IsValid()) return;

        EntityController.DeselectEntity(entity);
        EditorDataStore.EntityState = default;
        EditorDataStore.SelectedSceneObj = default;
        EditorDataStore.EntityState.ComponentRef = 0;
    }

    internal static void CommitEntity()
    {
        var entity = EditorDataStore.SelectedSceneObj;
        if (!entity.IsValid())
        {
            ConsoleGateway.LogPlain("Invalid selected entity for commit");
            return;
        }

        EntityController.Commit(entity, in EditorDataStore.EntityState);
    }

    internal static void RefreshEntity()
    {
        var entity = EditorDataStore.SelectedSceneObj;
        if (!entity.IsValid())
        {
            ConsoleGateway.LogPlain("Invalid selected entity for refresh");
            return;
        }

        EntityController.Fetch(entity, ref EditorDataStore.EntityState);
    }

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

    internal static void CommitCamera()
    {
        _durationProfileTimer.Begin();
        WorldController.CommitCamera(EditorDataStore.Slot<EditorCameraState>.GetView());
        _durationProfileTimer.EndPrint();
    }

    private static DurationProfileTimer _durationProfileTimer = new DurationProfileTimer(TimeSpan.FromSeconds(2));
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