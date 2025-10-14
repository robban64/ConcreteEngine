using ConcreteEngine.Core.Assets.Data;

namespace ConcreteEngine.Core.Assets.Textures;

internal sealed class TextureLoaderModule(AssetGfxUploader uploader)
{
    private TextureLoader _loader = new();

    public Texture2D LoadTexture2D(AssetId id, TextureManifestRecord manifest, out AssetFileSpec[] fileSpecs)
    {
        var payload = _loader.LoadTexture(manifest);
        uploader.UploadTexture(payload, out var info);
        fileSpecs = [payload.FileSpec];

        var texture = new Texture2D
        {
            RawId = id,
            Name = manifest.Name,
            ResourceId = info.TextureId,
            Width = info.Width,
            Height = info.Height,
            IsCoreAsset = false,
            Generation = 0
        };

        if (payload.Data is { } tData)
            texture.SetPixelData(tData);

        return texture;
    }

    public CubeMap LoadCubeMap(AssetId id, CubeMapManifestRecord manifest, out AssetFileSpec[] fileSpecs)
    {
        var payload = _loader.LoadCubeMap(manifest);
        uploader.UploadCubeMap(payload, out var info);
        fileSpecs = payload.FaceFiles;

        return new CubeMap
        {
            RawId = id,
            Name = manifest.Name,
            ResourceId = info.TextureId,
            Size = info.Size,
            IsCoreAsset = false,
            Generation = 0
        };
    }


    public void Unload()
    {
        _loader = null!;
    }
}