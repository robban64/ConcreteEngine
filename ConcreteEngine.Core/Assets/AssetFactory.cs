using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;

namespace ConcreteEngine.Core.Assets;
/*
internal sealed class AssetFactory(AssetStore store, AssetGfxUploader uploader)
{
    private readonly AssetStore _store = store;
      private  readonly AssetGfxUploader uploader = uploader;
      
      private readonly TextureLoader _textureLoader;

    public Texture2D BuildTexture(
        TextureManifestRecord manifestRecord,
        TextureLoader loader,
        AssetGfxUploader uploader)
    {
        var texture = _store.Register(manifestRecord,(id, manifest) =>
        {
            var payload = loader.LoadTexture(manifest);
            uploader.UploadTexture(payload, out var info);

            return new Texture2D
            {
                Id = id,
                Name = manifest.Name,
                ResourceId = info.TextureId,
                Width = info.Width,
                Height = info.Height,
                IsCoreAsset = false,
                Generation = 0
            };
        });
        
        if (payload.Data is { } tData)
            texture.SetPixelData(tData);

        return texture;

    }
    
    private Texture2D BuildTexture(
        TextureManifestRecord manifestRecord,
        TextureLoader loader,
        AssetGfxUploader uploader)
    {
        var texture = _store.Register(manifestRecord,(id, manifest) =>
        {
            var payload = loader.LoadTexture(manifest);
            uploader.UploadTexture(payload, out var info);

            return new Texture2D
            {
                Id = id,
                Name = manifest.Name,
                ResourceId = info.TextureId,
                Width = info.Width,
                Height = info.Height,
                IsCoreAsset = false,
                Generation = 0
            };
        });
        
        if (payload.Data is { } tData)
            texture.SetPixelData(tData);

        return texture;

    }

    public CubeMap BuildCubeMap(
        CubeMapManifestRecord manifestRecord,
        TextureLoader loader,
        AssetGfxUploader uploader)
    {
        return _store.Register(manifestRecord,(id,manifest) =>
        {
            var payload = loader.LoadCubeMap(manifest);
            uploader.UploadCubeMap(payload, out var info);

            registerFiles(id, payload.FaceFiles);
            return new CubeMap
            {
                Id = id,
                Name = manifest.Name,
                ResourceId = info.TextureId,
                Size = info.Size,
                IsCoreAsset = false,
                Generation = 0
            };
        });
    }


    public Mesh BuildMesh(
        MeshManifestRecord manifest,
        MeshLoader loader,
        AssetGfxUploader uploader)
    {
        return _store.Register((id) =>
        {
            var payload = loader.LoadMesh(manifest);
            uploader.UploadMesh(payload, out var info);
            registerFiles(id, [payload.FileSpec]);
            return new Mesh
            {
                Id = id,
                ResourceId = info.MeshId,
                Name = manifest.Name,
                DrawCount = info.DrawCount,
                IsCoreAsset = false,
                Generation = 0
            };
        });
    }

    public Shader BuildShader(
        ShaderManifestRecord manifest,
        ShaderLoader loader,
        AssetGfxUploader uploader)
    {
        return _store.Register((id) =>
        {
            var payload = loader.LoadShader(manifest);
            uploader.UploadShader(payload, out var info);

            registerFiles(id, [payload.VertexFileSpec, payload.FragmentFileSpec]);
            return new Shader
            {
                Id = id,
                ResourceId = info.ShaderId,
                Name = manifest.Name,
                Samplers = info.Samplers,
                IsCoreAsset = false,
                Generation = 0
            };
        });
    }
}*/