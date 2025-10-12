using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetLoader
{
    private AssetFactory _factory = null!;

    private TextureLoaderModule _textureLoader = null!;
    private MeshLoaderModule _meshLoader = null!;
    private ShaderLoaderModule _shaderLoader = null!;

    public Shader LoadShader(ShaderManifestRecord manifest)
        => _shaderLoader.LoadShader(_factory, manifest);

    public Texture2D LoadTexture2D(TextureManifestRecord manifest)
        => _textureLoader.LoadTexture2D(_factory, manifest);

    public CubeMap LoadCubeMap(CubeMapManifestRecord manifest)
        => _textureLoader.LoadCubeMap(_factory, manifest);

    public Mesh LoadMesh(MeshManifestRecord manifest)
        => _meshLoader.LoadMesh(_factory, manifest);


    public void ActivateLoader(AssetFactory factory, AssetGfxUploader gfx)
    {
        _textureLoader = new TextureLoaderModule(gfx.UploadTexture, gfx.UploadCubeMap);
        _meshLoader = new MeshLoaderModule(gfx.UploadMesh);
        _shaderLoader = new ShaderLoaderModule(gfx.UploadShader);
        
        _shaderLoader.Prepare();
    }

    
    public void DeactivateLoader()
    {
        _meshLoader.Unload();
        _textureLoader.Unload();
        _shaderLoader.Unload();
        
        _meshLoader = null!;
        _textureLoader = null!;
        _shaderLoader = null!;
        _factory = null!;
    }


    private sealed class TextureLoaderModule(
        AssetUploaderDel<TexturePayload, TextureCreationInfo> uploadTexture,
        AssetUploaderDel<CubeMapPayload, CubeMapCreationInfo> uploadCubeMap)
    {
        private TextureLoader _loader = new();
        private AssetUploaderDel<TexturePayload, TextureCreationInfo> _uploadTexture = uploadTexture;
        private AssetUploaderDel<CubeMapPayload, CubeMapCreationInfo> _uploadCubeMap = uploadCubeMap;

        public Texture2D LoadTexture2D(AssetFactory factory, TextureManifestRecord manifest)
            => factory.BuildTexture(manifest, _loader.LoadTexture, _uploadTexture);

        public CubeMap LoadCubeMap(AssetFactory factory, CubeMapManifestRecord manifest)
            => factory.BuildCubeMap(manifest, _loader.LoadCubeMap, _uploadCubeMap);

 
        public void Unload()
        {
            _loader = null!;
            _uploadTexture = null!;
            _uploadCubeMap = null!;
        }
    }


    private sealed class MeshLoaderModule(AssetUploaderDel<MeshResultPayload, MeshCreationInfo> uploader)
    {
        private MeshLoader _loader = new();
        private AssetUploaderDel<MeshResultPayload, MeshCreationInfo> _uploader = uploader;

        public Mesh LoadMesh(AssetFactory factory, MeshManifestRecord manifest)
            => factory.BuildMesh(manifest, _loader.LoadMesh, _uploader);

        public void Unload()
        {
            _loader.ClearCache();
            _loader = null!;
            _uploader = null!;
        }
    }

    private sealed class ShaderLoaderModule(AssetUploaderDel<ShaderPayload, ShaderCreationInfo> uploader)
    {
        private ShaderLoader _loader = new();
        private AssetUploaderDel<ShaderPayload, ShaderCreationInfo> _uploader = uploader;

        public Shader LoadShader(AssetFactory factory, ShaderManifestRecord manifest)
            => factory.BuildShader(manifest, _loader.LoadShader, _uploader);
        
        public void Prepare() => _loader.Prepare();
        
        public void Unload()
        {
            _loader.ClearCache();
            _loader = null!;
            _uploader = null!;
        }
    }
}

/*
internal void Start(GfxContext gfx,  AssetManifestBundle assetRecords)
{
    IsLoading = true;
    _factory = new AssetFactory();
    _uploader = new AssetGfxUploader(gfx);
    _loader = new AssetProcessor(AssetPaths.AssetFolder, _uploader);
    _loader.Start(assetRecords);
}

internal bool ProcessLoader(int n, AssetSystem assetSystem)
{
    ArgumentNullException.ThrowIfNull(assetSystem);
    InvalidOpThrower.ThrowIfNot(IsLoading);

    for (var i = 0; i < n; i++)
    {
        if (_loader!.Process(out var finalEntry)) return true;
        if (finalEntry is not null)
            _assemblerRegistry!.AssembleAsset(finalEntry, assetSystem);
    }

    return false;
}*/