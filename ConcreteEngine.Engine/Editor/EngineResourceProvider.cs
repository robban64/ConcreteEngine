using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Editor;

internal static class EngineResourceProvider
{
    private static AssetSystem _assetSystem = null!;
    private static EntityApiController _entityController = null!;
    private static WorldApiController _worldApiController = null!;

    internal static void Attach(AssetSystem assetSystem, EntityApiController entityController,
         WorldApiController worldApiController)
    {
        _assetSystem = assetSystem;
        _entityController = entityController;
        _worldApiController = worldApiController;
    }


    public static List<EditorEntityResource> CreateEntityList()
    {
        var entities = _entityController.CreateEntityList();
        Logger.LogString(LogScope.Engine, $"Editor Entities loaded - {entities.Count}");
        return entities;
    }
}