using System.Runtime.InteropServices;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Store;

internal static partial class ManagedStore
{
    private static class Loader
    {
        public static void LoadAll()
        {
            var assets = EngineController.AssetController.LoadAssetList();
            var entities = EditorApi.LoadEntityResources();
            var sceneObjects = EngineController.SceneController.LoadSceneObjectList();

            var totalCount = assets.Count + entities.Count + sceneObjects.Count;
            Resources.EnsureCapacity(int.Max(totalCount, 32));
            ByName.EnsureCapacity(int.Max(totalCount, 32));

            foreach (var res in assets) Register(res);
            foreach (var res in entities) Register(res);
            foreach (var res in sceneObjects) Register(res);

            _assetResources = assets;
            _entityResources = entities;
            _sceneObjects = sceneObjects;
            
            CreateAssetRanges();
        }


        private static void CreateAssetRanges()
        {
            var span = CollectionsMarshal.AsSpan(_assetResources);

            var prevCategory = EditorAssetCategory.None;
            var startIndex = 0;
            for (int i = 1; i < span.Length; i++)
            {
                var category = span[i].AssetCategory;
                if (span[i].AssetCategory == prevCategory) continue;
                AssetRanges[(int)prevCategory] = (startIndex, i - startIndex);

                prevCategory = category;
                startIndex = i;
            }

            AssetRanges[(int)prevCategory] = (startIndex, span.Length - startIndex);
        }
    }
}