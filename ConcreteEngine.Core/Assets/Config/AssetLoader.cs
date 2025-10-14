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
}