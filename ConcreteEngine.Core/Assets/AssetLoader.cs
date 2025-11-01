#region

using ConcreteEngine.Common;
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
    private AssetStore? _store;
    private AssetDataProvider? _dataProvider;
    
    private AssetGfxUploader? _gfxUploader;
    
    private TextureLoaderModule? _textureLoader;
    private ModelLoaderModule? _meshLoader;
    private ShaderLoaderModule? _shaderLoader;
    private MaterialLoader? _materialLoader;

    private AssetFileAssembleDel<Shader, ShaderDescriptor>? _loadShaderDel;
    private AssetFileAssembleDel<Texture2D, TextureDescriptor>? _loadTextureDel;
    private AssetFileAssembleDel<CubeMap, CubeMapDescriptor>? _loadCubeMapDel;
    private AssetFileAssembleDel<Model, MeshDescriptor>? _loadMeshDel;

    public bool IsActive { get; private set; }

    public Shader LoadShader(ShaderDescriptor manifest, bool isCoreAsset)
        => _store!.RegisterWithFiles(manifest, isCoreAsset, _loadShaderDel!);

    public Texture2D LoadTexture2D(TextureDescriptor manifest,bool isCoreAsset) =>
        _store!.RegisterWithFiles(manifest, isCoreAsset, _loadTextureDel!);

    public CubeMap LoadCubeMap(CubeMapDescriptor manifest,bool isCoreAsset)
        => _store!.RegisterWithFiles(manifest,isCoreAsset, _loadCubeMapDel!);

    public Model LoadMesh(MeshDescriptor manifest,bool isCoreAsset)
    {
        if (manifest.LoadMode == AssetLoadingMode.MemoryOnly)
        {
            return null!;
        }
        return _store!.RegisterWithFiles(manifest,isCoreAsset, _loadMeshDel!);
    }

    public List<MaterialTemplate> LoadAllMaterials(MaterialManifest manifest)
        => _materialLoader!.LoadMaterials(_store!, manifest.Records)!;

    public void ReloadShader(Shader shader)
    {
        InvalidOpThrower.ThrowIf(!IsActive, nameof(IsActive));
        _shaderLoader ??= new ShaderLoaderModule(_gfxUploader!);
        _store!.Reload(shader, _shaderLoader!.ReloadShader);
    }


    public void ActivateFullLoader(AssetStore store, AssetGfxUploader gfx)
    {
        InvalidOpThrower.ThrowIf(IsActive);

        _store = store;

        _gfxUploader = gfx;
        _textureLoader ??= new TextureLoaderModule(gfx);
        _meshLoader ??= new ModelLoaderModule(gfx);
        _shaderLoader ??= new ShaderLoaderModule(gfx);
        _materialLoader ??= new MaterialLoader();

        _loadShaderDel ??= _shaderLoader.LoadShader;
        _loadTextureDel ??= _textureLoader.LoadTexture2D;
        _loadCubeMapDel ??= _textureLoader.LoadCubeMap;
        _loadMeshDel ??= _meshLoader.LoadModel;

        _shaderLoader.Prepare();

        IsActive = true;
    }

    public void ActivateLazyLoader(AssetStore store, AssetGfxUploader gfx)
    {
        IsActive = true;
        _store = store;
        _gfxUploader = gfx;
        /*
        _textureLoader ??= new TextureLoaderModule(gfx);
        _meshLoader ??= new ModelLoaderModule(gfx);
        _shaderLoader ??= new ShaderLoaderModule(gfx);
        _materialLoader ??= new MaterialLoader();
        */
    }


    public void DeactivateLoader()
    {
        _loadShaderDel = null;
        _loadTextureDel = null;
        _loadCubeMapDel = null;
        _loadMeshDel = null;

        _meshLoader?.Unload();
        _textureLoader?.Unload();
        _shaderLoader?.Unload();

        _meshLoader = null;
        _textureLoader = null;
        _shaderLoader = null;
        _materialLoader = null;

        _gfxUploader = null;
        
        IsActive = false;
    }
}