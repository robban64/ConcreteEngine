#region

using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Shared.Rendering;

#endregion

namespace ConcreteEngine.Editor;

public static class EngineController
{
    public static IEngineWorldController WorldController = null!;
    public static IEngineInteractionController InteractionController = null!;
    public static IEngineEntityController EntityController = null!;

    private static EditorId SelectedEntity => EditorDataStore.SelectedEntity;
    private static EditorId ComponentRef => EditorDataStore.EntityState.ComponentRef;

    internal static void SelectEntity(EditorId entity)
    {
        if (entity == SelectedEntity) return;
        if (!entity.IsValid)
        {
            ConsoleService.SendLog("Invalid selected entity");
            return;
        }

        if (SelectedEntity.IsValid)
            EntityController.DeselectEntity(SelectedEntity);

        EntityController.SelectEntity(entity, out EditorDataStore.EntityState);
        EditorDataStore.SelectedEntity = entity;

        var entityObj = EditorManagedStore.Get<EditorEntityResource>(entity);

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
            default: EditorDataStore.EntityState.ComponentRef = EditorId.Empty; break;
        }
    }

    internal static void DeSelectEntity()
    {
        var entity = SelectedEntity;
        if (!entity.IsValid) return;

        EntityController.DeselectEntity(entity);
        EditorDataStore.EntityState = default;
        EditorDataStore.SelectedEntity = EditorId.Empty;
        EditorDataStore.EntityState.ComponentRef = EditorId.Empty;
    }

    internal static void CommitEntity()
    {
        var entity = SelectedEntity;
        if (!entity.IsValid)
        {
            ConsoleService.SendLog("Invalid selected entity for commit");
            return;
        }

        EntityController.Commit(entity, in EditorDataStore.EntityState);
    }

    internal static void RefreshEntity()
    {
        var entity = SelectedEntity;
        if (!entity.IsValid)
        {
            ConsoleService.SendLog("Invalid selected entity for refresh");
            return;
        }

        EntityController.Fetch(entity, ref EditorDataStore.EntityState);
    }

    internal static void FetchAnimation()
    {
        var entity = SelectedEntity;
        if (!entity.IsValid || !ComponentRef.IsValid) return;
        EntityController.FetchAnimation(entity, ref EditorDataStore.AnimationState);
    }

    internal static void CommitAnimation()
    {
        var entity = SelectedEntity;
        if (!entity.IsValid || !ComponentRef.IsValid) return;
        EntityController.CommitAnimation(entity, in EditorDataStore.AnimationState);
    }

    internal static void FetchParticle()
    {
        var entity = SelectedEntity;
        if (!entity.IsValid || !ComponentRef.IsValid) return;
        EntityController.FetchParticle(entity, ref EditorDataStore.ParticleState);
    }

    internal static void CommitParticle()
    {
        var entity = SelectedEntity;
        if (!entity.IsValid || !ComponentRef.IsValid) return;
        EntityController.CommitParticle(entity, in EditorDataStore.ParticleState);
    }

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