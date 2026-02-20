using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Engine.Assets;

namespace ConcreteEngine.Engine.Editor;

internal static class InspectorBinder
{
    internal static void RegisterProviders(AssetStore assetStore)
    {
        InspectorProvider.Register(typeof(AssetFileSpec), assetStore, static (provider, target) =>
        {
            var assetStore = (AssetStore)provider;
            var asset = (AssetObject)target;
            assetStore.TryGetFileIds(asset.Id, out var fileIds);

            if (fileIds.Length == 0 || !assetStore.TryGet(asset.Id, out _)) return Array.Empty<AssetFileSpec>();

            var result = new AssetFileSpec[fileIds.Length];
            for (var i = 0; i < fileIds.Length; i++)
                assetStore.TryGetFileEntry(fileIds[i], out result[i]);

            return result;

        });
    }
    
    internal static void RegisterTypes()
    {
        /*
        InspectorRegistry.RegisterType(typeof(Model));
        InspectorRegistry.RegisterType(typeof(MeshEntry));
        InspectorRegistry.RegisterType(typeof(ModelAnimation));
        InspectorRegistry.RegisterType(typeof(AnimationClip));
        InspectorRegistry.RegisterType(typeof(MeshInfo));
        InspectorRegistry.RegisterType(typeof(ModelInfo));
        */

    }
}