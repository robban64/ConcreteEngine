#region

using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.Loader;

internal sealed class ModelLoaderModule
{
    private ModelLoader _loader;
    private ModelLoaderState _state;

    public ModelLoaderModule(AssetGfxUploader uploader)
    {
        _state = new ModelLoaderState();
        _loader = new ModelLoader(uploader, _state);
    }

    public Model LoadModel(
        AssetId assetId,
        MeshDescriptor manifest,
        bool isCoreAsset,
        Action<ReadOnlySpan<IAssetEmbeddedDescriptor>> uploadEmbedded,
        out AssetFileSpec[] fileSpecs)
    {
        var refId = AssetRef<Model>.Make(assetId);

        var result = _loader.LoadMesh(refId, manifest.Name, manifest.Filename, out fileSpecs);
        uploadEmbedded(result.EmbeddedTextures);
        uploadEmbedded(result.EmbeddedMaterials);

        result.Animation?.ModelName = manifest.Name;

        return new Model
        {
            RawId = assetId,
            Name = manifest.Name,
            MeshParts = result.MeshParts,
            Animation = result.Animation,
            DrawCount = result.DrawCount,
            IsCoreAsset = isCoreAsset,
            Bounds = result.Bounds
        };
    }

    public void ClearState()
    {
        _state.Clear();
    }

    public void Teardown()
    {
        _state.Clear();
        _loader.Teardown();
        _state = null!;
        _loader = null!;
    }
}