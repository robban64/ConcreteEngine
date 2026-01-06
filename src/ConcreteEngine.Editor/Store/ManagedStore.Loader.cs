using System.Runtime.InteropServices;

namespace ConcreteEngine.Editor.Store;

internal static partial class ManagedStore
{
    private static class Loader
    {
        public static void LoadAll()
        {
            var entities = EngineController.EntityController.LoadEntityList();
            var sceneObjects = EngineController.SceneController.LoadSceneObjectList();

            var totalCount =  entities.Count + sceneObjects.Count;
            Resources.EnsureCapacity(int.Max(totalCount, 32));
            ByName.EnsureCapacity(int.Max(totalCount, 32));

            foreach (var res in entities) Register(res);
            foreach (var res in sceneObjects) Register(res);

            _entityResources = entities;
            _sceneObjects = sceneObjects;
        }

    }
}