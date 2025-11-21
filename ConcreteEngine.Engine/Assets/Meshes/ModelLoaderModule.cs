#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Engine.Assets.Meshes;

internal sealed class ModelLoaderModule
{
    private MeshLoader _loader;

    public ModelLoaderModule(AssetGfxUploader uploader)
    {
        _loader = new MeshLoader(uploader.UploadMesh, uploader.UploadMesh);
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