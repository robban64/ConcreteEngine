#region

using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;

#endregion

namespace ConcreteEngine.Engine.Assets.Textures;

internal sealed class TextureLoaderModule
{
    private TextureLoader _loader;

    public TextureLoaderModule(AssetGfxUploader uploader)
    {
        _loader = new TextureLoader(uploader);
    }
    
    public Texture2D LoadEmbeddedTexture(AssetId id, TextureEmbeddedDescriptor descriptor, IAssetStore assetStore)
    {
        var result = _loader.LoadEmbeddedTexture(descriptor);

        var texture = new Texture2D
        {
            RawId = id,
            Name = descriptor.EmbeddedName,
            ResourceId = result.CreationInfo.TextureId,
            Width = result.CreationInfo.Width,
            Height = result.CreationInfo.Height,
            IsCoreAsset = false,
            SlotKind = descriptor.SlotKind
        };

        return texture;
    }

    public Texture2D LoadTexture2D(AssetId id, TextureDescriptor manifest, bool isCoreAsset,
        out AssetFileSpec[] fileSpecs)
    {
        var result = _loader.LoadTexture(manifest);
        fileSpecs = [result.FileSpec];

        var texture = new Texture2D
        {
            RawId = id,
            Name = manifest.Name,
            ResourceId = result.CreationInfo.TextureId,
            Width = result.CreationInfo.Width,
            Height = result.CreationInfo.Height,
            IsCoreAsset = isCoreAsset
        };

        if (result.Data is { } tData)
            texture.SetPixelData(tData);

        return texture;
    }

    public CubeMap LoadCubeMap(AssetId id, CubeMapDescriptor manifest, bool isCoreAsset, out AssetFileSpec[] fileSpecs)
    {
        var result = _loader.LoadCubeMap(manifest);
        fileSpecs = result.FaceFiles;

        return new CubeMap
        {
            RawId = id,
            Name = manifest.Name,
            ResourceId = result.CreationInfo.TextureId,
            Size = result.CreationInfo.Size,
            IsCoreAsset = isCoreAsset
        };
    }


    public void Unload()
    {
        _loader = null!;
    }
}