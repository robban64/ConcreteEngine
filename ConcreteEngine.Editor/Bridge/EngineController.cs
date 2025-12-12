using ConcreteEngine.Editor.Store;

namespace ConcreteEngine.Editor.Bridge;

internal sealed class EngineController
{
    private static EditorId SelectedEntity => EditorDataStore.State.SelectedEntity;

    internal static void SelectEntity(EditorId entity)
    {
        if (entity == SelectedEntity) return;
        if (!entity.IsValid)
        {
            ConsoleService.SendLog("Invalid selected entity");
            return;
        }

        if (SelectedEntity.IsValid)
            EditorApi.EntityController.DeselectEntity(SelectedEntity);

        EditorApi.EntityController.SelectEntity(entity, out EditorDataStore.State.EntityState);
        EditorDataStore.State.SelectedEntity = entity;
    }

    internal static void DeSelectEntity()
    {
        var entity = SelectedEntity;
        if (!entity.IsValid) return;

        EditorApi.EntityController.DeselectEntity(entity);
        EditorDataStore.State.EntityState = default;
        EditorDataStore.State.SelectedEntity = EditorId.Empty;
    }

    internal static void CommitEntity()
    {
        var entity = SelectedEntity;
        if (!entity.IsValid)
        {
            ConsoleService.SendLog("Invalid selected entity for commit");
            return;
        }

        EditorApi.EntityController.Commit(entity, in EditorDataStore.State.EntityState);
    }

    internal static void RefreshEntity()
    {
        var entity = SelectedEntity;
        if (!entity.IsValid)
        {
            ConsoleService.SendLog("Invalid selected entity for refresh");
            return;
        }

        EditorApi.EntityController.Fetch(entity, ref EditorDataStore.State.EntityState);
    }
}