using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Editor.Store;

internal static partial class ManagedStore
{
    private static class Loader
    {
        public static void LoadAll()
        {
            var assets = EngineController.AssetController.LoadAssetList();
            var entities = EngineController.EntityController.LoadEntityList();
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

            _assetResources.Sort();

            CreateAssetRanges();
        }


        private static void CreateAssetRanges()
        {
            var span = CollectionsMarshal.AsSpan(_assetResources);

            var prevKind = AssetKind.Unknown;
            var startIndex = 0;
            for (int i = 1; i < span.Length; i++)
            {
                var kind = span[i].Kind;
                if (span[i].Kind == prevKind) continue;
                AssetRanges[(int)prevKind] = (startIndex, i - startIndex);
                prevKind = kind;
                startIndex = i;
            }

            AssetRanges[(int)prevKind] = (startIndex, span.Length - startIndex);
        }
    }
}