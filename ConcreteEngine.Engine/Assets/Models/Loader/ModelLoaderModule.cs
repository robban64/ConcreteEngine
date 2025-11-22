#region

using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.Loader;

internal sealed class ModelLoaderModule
{
    private ModelLoader _loader;

    public ModelLoaderModule(AssetGfxUploader uploader)
    {
        _loader = new ModelLoader(uploader);
    }

    public Model LoadModel(AssetId assetId, MeshDescriptor manifest, bool isCoreAsset, out AssetFileSpec[] fileSpecs)
    {
        var refId = AssetRef<Model>.Make(assetId);

        var result  = _loader.LoadMesh(refId, manifest.Name, manifest.Filename, out fileSpecs);

        return new Model
        {
            RawId = assetId,
            Name = manifest.Name,
            MeshParts = result.MeshParts,
            Animation =  result.Animation,
            DrawCount = result.DrawCount,
            IsCoreAsset = isCoreAsset,
            Bounds = result.Bounds
        };
    }

    public void Unload()
    {
        _loader.ClearCache();
        _loader = null!;
    }

}