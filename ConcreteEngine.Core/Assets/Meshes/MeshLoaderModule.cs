#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Assets.Internal;

#endregion

namespace ConcreteEngine.Core.Assets.Meshes;

internal sealed class MeshLoaderModule(AssetGfxUploader uploader)
{
    private MeshLoader _loader = new();

    public Mesh LoadMesh(AssetId assetId, MeshDescriptor manifest, out AssetFileSpec[] fileSpecs)
    {
        var payload = _loader.LoadMesh(manifest);
        uploader.UploadMesh(payload, out var info);
        fileSpecs = [payload.FileSpec];

        return new Mesh
        {
            RawId = assetId,
            ResourceId = info.MeshId,
            Name = manifest.Name,
            DrawCount = info.DrawCount,
            IsCoreAsset = false,
            Generation = 0
        };
    }

    public void Unload()
    {
        _loader.ClearCache();
        _loader = null!;
    }
}