namespace ConcreteEngine.Engine.Gateway;
/*
internal static class InspectorBinder
{
    internal static void RegisterProviders(AssetSystem assetSystem)
    {
        InspectorProvider.Register(typeof(AssetFile), assetSystem, static (provider, target) =>
        {
            var assetSystem = (AssetSystem)provider;
            var asset = (AssetObject)target;
            assetSystem.Files.TryGetFileBindings(asset.Id, out var fileIds);

            if (fileIds.Length == 0 || !assetSystem.Assets.TryGet(asset.Id, out _)) return Array.Empty<AssetFile>();

            var result = new AssetFile[fileIds.Length];
            for (var i = 0; i < fileIds.Length; i++)
                assetSystem.Files.TryGetFile(fileIds[i], out result[i]);

            return result;
        });
    }

    internal static void RegisterTypes()
    {

        InspectorRegistry.RegisterType(typeof(Model));
        InspectorRegistry.RegisterType(typeof(MeshEntry));
        InspectorRegistry.RegisterType(typeof(ModelAnimation));
        InspectorRegistry.RegisterType(typeof(AnimationClip));
        InspectorRegistry.RegisterType(typeof(MeshInfo));
        InspectorRegistry.RegisterType(typeof(ModelInfo));
        
    }
}
*/