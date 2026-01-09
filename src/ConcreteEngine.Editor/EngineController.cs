using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor;

public static class EngineController
{
    public static EngineWorldController WorldController = null!;
    public static EngineInteractionController InteractionController = null!;
    public static EngineSceneController SceneController = null!;
    public static EngineAssetController AssetController = null!;
    
    internal static void SelectSceneObject(SceneObjectId id)
    {
        if (id == EditorDataStore.SelectedSceneObj) return;
        if (!id.IsValid())
        {
            ConsoleGateway.LogPlain("Invalid selected SceneObjectId");
            return;
        }

        if (EditorDataStore.SelectedSceneObj.IsValid())
            SceneController.Deselect(EditorDataStore.SelectedSceneObj);

        SceneController.Select(id);
        EditorDataStore.SelectedSceneObj = id;
        
        EditorDataStore.SceneObjectView = SceneController.GetSceneObjectView(id);
        
        SceneController.FetchTransform(id, ref EditorDataStore.Slot<TransformStable>.State);

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

    internal static void DeSelectSceneObject()
    {
        var id = EditorDataStore.SelectedSceneObj;
        if (!id.IsValid()) return;

        SceneController.Deselect(id);
        EditorDataStore.Slot<TransformStable>.State = default;
        EditorDataStore.SelectedSceneObj = default;
    }

    internal static void CommitSceneObject()
    {
        var id = EditorDataStore.SelectedSceneObj;
        if (!id.IsValid())
        {
            ConsoleGateway.LogPlain("Invalid selected entity for commit");
            return;
        }

        SceneController.CommitTransform(id, in EditorDataStore.Slot<TransformStable>.State);
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
        WorldController.CommitWorldRenderParams(EditorDataStore.Slot<EditorVisualState>.GetView());
    }

    internal static void FetchWorldParams()
    {
        WorldController.FetchWorldRenderParams(EditorDataStore.Slot<EditorVisualState>.GetView());
    }
}