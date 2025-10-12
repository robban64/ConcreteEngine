using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetFactory(AssetStore store)
{
    public Texture2D BuildTexture(
        TextureManifestRecord manifest,
        AssetLoaderDel<TextureManifestRecord, TexturePayload> loader,
        AssetUploaderDel<TexturePayload, TextureCreationInfo> uploader)
    {
        return store.Register((id, registerFiles) =>
        {
            var payload = loader(manifest);
            uploader(payload, out var info);

            registerFiles(id, [payload.FileSpec]);
            var texture = new Texture2D
            {
                Id = id,
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
        });
    }

    public CubeMap BuildCubeMap(
        CubeMapManifestRecord manifest,
        AssetLoaderDel<CubeMapManifestRecord, CubeMapPayload> loader,
        AssetUploaderDel<CubeMapPayload, CubeMapCreationInfo> uploader)
    {
        return store.Register((id, registerFiles) =>
        {
            var payload = loader(manifest);
            uploader(payload, out var info);

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
        AssetLoaderDel<MeshManifestRecord, MeshResultPayload> loader,
        AssetUploaderDel<MeshResultPayload, MeshCreationInfo> uploader)
    {
        return store.Register((id, registerFiles) =>
        {
            var payload = loader(manifest);
            uploader(payload, out var info);
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
        AssetLoaderDel<ShaderManifestRecord, ShaderPayload> loader,
        AssetUploaderDel<ShaderPayload, ShaderCreationInfo> uploader)
    {
        return store.Register((id, registerFiles) =>
        {
            var payload = loader(manifest);
            uploader(payload, out var info);

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
}