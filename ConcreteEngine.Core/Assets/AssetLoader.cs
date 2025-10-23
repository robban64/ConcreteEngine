#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Assets.Internal;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;

#endregion

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetLoader
{
    private AssetStore _store = null!;

    private TextureLoaderModule _textureLoader = null!;
    private MeshLoaderModule _meshLoader = null!;
    private ShaderLoaderModule _shaderLoader = null!;
    private MaterialLoader _materialLoader = null!;

    private AssetFileAssembleDel<Shader, ShaderDescriptor> _loadShaderDel = null!;
    private AssetFileAssembleDel<Texture2D, TextureDescriptor> _loadTextureDel = null!;
    private AssetFileAssembleDel<CubeMap, CubeMapDescriptor> _loadCubeMapDel = null!;
    private AssetFileAssembleDel<Mesh, MeshDescriptor> _loadMeshDel = null!;

    public Shader LoadShader(ShaderDescriptor manifest)
        => _store.RegisterWithFiles(manifest, _loadShaderDel);

    public Texture2D LoadTexture2D(TextureDescriptor manifest) =>
        _store.RegisterWithFiles(manifest, _loadTextureDel);

    public CubeMap LoadCubeMap(CubeMapDescriptor manifest)
        => _store.RegisterWithFiles(manifest, _loadCubeMapDel);

    public Mesh LoadMesh(MeshDescriptor manifest)
        => _store.RegisterWithFiles(manifest, _loadMeshDel);

    public List<MaterialTemplate> LoadAllMaterials(MaterialManifest manifest)
        => _materialLoader.LoadMaterials(_store, manifest.Records)!;

    public void ReloadShader(Shader shader, AssetFileEntry vertFile, AssetFileEntry fragFile)
    {
        _shaderLoader.ReloadShader(shader, vertFile, fragFile, out var specs);
    }


    public void ActivateLoader(AssetStore store, AssetGfxUploader gfx)
    {
        _store = store;

        _textureLoader = new TextureLoaderModule(gfx);
        _meshLoader = new MeshLoaderModule(gfx);
        _shaderLoader = new ShaderLoaderModule(gfx);
        _materialLoader = new MaterialLoader();

        _loadShaderDel = _shaderLoader.LoadShader;
        _loadTextureDel = _textureLoader.LoadTexture2D;
        _loadCubeMapDel = _textureLoader.LoadCubeMap;
        _loadMeshDel = _meshLoader.LoadMesh;

        _shaderLoader.Prepare();
    }


    public void DeactivateLoader()
    {
        _loadShaderDel = null!;
        _loadTextureDel = null!;
        _loadCubeMapDel = null!;
        _loadMeshDel = null!;

        _meshLoader.Unload();
        _textureLoader.Unload();
        _shaderLoader.Unload();

        _meshLoader = null!;
        _textureLoader = null!;
        _shaderLoader = null!;
    }
}