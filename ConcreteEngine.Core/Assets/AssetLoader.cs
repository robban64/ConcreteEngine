using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetLoader
{
    private AssetStore _store = null!;

    private TextureLoaderModule _textureLoader = null!;
    private MeshLoaderModule _meshLoader = null!;
    private ShaderLoaderModule _shaderLoader = null!;

    private AssetFileAssembleDel<Shader, ShaderManifestRecord> _loadShaderDel = null!;
    private AssetFileAssembleDel<Texture2D, TextureManifestRecord> _loadTextureDel = null!;
    private AssetFileAssembleDel<CubeMap, CubeMapManifestRecord> _loadCubeMapDel = null!;
    private AssetFileAssembleDel<Mesh, MeshManifestRecord> _loadMeshDel = null!;

    public Shader LoadShader(ShaderManifestRecord manifest)
        => _store.RegisterWithFiles(manifest, _loadShaderDel);

    public Texture2D LoadTexture2D(TextureManifestRecord manifest) =>
        _store.RegisterWithFiles(manifest, _loadTextureDel);

    public CubeMap LoadCubeMap(CubeMapManifestRecord manifest)
        => _store.RegisterWithFiles(manifest, _loadCubeMapDel);

    public Mesh LoadMesh(MeshManifestRecord manifest)
        => _store.RegisterWithFiles(manifest, _loadMeshDel);


    public void ActivateLoader(AssetStore store, AssetGfxUploader gfx)
    {
        _store = store;

        _textureLoader = new TextureLoaderModule(gfx);
        _meshLoader = new MeshLoaderModule(gfx);
        _shaderLoader = new ShaderLoaderModule(gfx);

        _loadShaderDel = _shaderLoader.LoadShader;
        _loadTextureDel = _textureLoader.LoadTexture2D;
        _loadCubeMapDel = _textureLoader.LoadCubeMap;
        _loadMeshDel =  _meshLoader.LoadMesh;

        _shaderLoader.Prepare();
    }


    public void DeactivateLoader()
    {
        _loadShaderDel = null!;
        _loadTextureDel = null!;
        _loadCubeMapDel = null!;
        _loadMeshDel =  null!;

        _meshLoader.Unload();
        _textureLoader.Unload();
        _shaderLoader.Unload();

        _meshLoader = null!;
        _textureLoader = null!;
        _shaderLoader = null!;
    }


    private sealed class TextureLoaderModule(AssetGfxUploader uploader)
    {
        private TextureLoader _loader = new();

        public Texture2D LoadTexture2D(AssetId id, TextureManifestRecord manifest, out AssetFileSpec[] fileSpecs)
        {
            var payload = _loader.LoadTexture(manifest);
            uploader.UploadTexture(payload, out var info);
            fileSpecs = [payload.FileSpec];

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
        }

        public CubeMap LoadCubeMap(AssetId id, CubeMapManifestRecord manifest, out AssetFileSpec[] fileSpecs)
        {
            var payload = _loader.LoadCubeMap(manifest);
            uploader.UploadCubeMap(payload, out var info);
            fileSpecs = payload.FaceFiles;

            return new CubeMap
            {
                Id = id,
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


    private sealed class MeshLoaderModule(AssetGfxUploader uploader)
    {
        private MeshLoader _loader = new();

        public Mesh LoadMesh(AssetId assetId, MeshManifestRecord manifest, out AssetFileSpec[] fileSpecs)
        {
            var payload = _loader.LoadMesh(manifest);
            uploader.UploadMesh(payload, out var info);
            fileSpecs = [payload.FileSpec];

            return new Mesh
            {
                Id = assetId,
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

    private sealed class ShaderLoaderModule(AssetGfxUploader uploader)
    {
        private ShaderLoader _loader = new();

        public Shader LoadShader(AssetId assetId, ShaderManifestRecord manifest,
            out AssetFileSpec[] fileSpecs)
        {
            var payload = _loader.LoadShader(manifest);
            uploader.UploadShader(payload, out var info);
            fileSpecs = [payload.VertexFileSpec, payload.FragmentFileSpec];
            return new Shader
            {
                Id = assetId,
                ResourceId = info.ShaderId,
                Name = manifest.Name,
                Samplers = info.Samplers,
                IsCoreAsset = false,
                Generation = 0
            };
        }

        public void Prepare() => _loader.Prepare();

        public void Unload()
        {
            _loader.ClearCache();
            _loader = null!;
        }
    }
}