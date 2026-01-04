using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Models.Loader;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets;

internal sealed class AssetLoader
{
    private AssetStore? _store;
    private AssetDataProvider? _dataProvider;

    private AssetGfxUploader? _gfxUploader;

    private TextureLoaderModule? _textureLoader;
    private ModelLoaderModule? _meshLoader;
    private ShaderLoaderModule? _shaderLoader;
    private MaterialLoader? _materialLoader;

    private LoadAssetDel<Shader, ShaderDescriptor>? _loadShaderDel;
    private LoadAssetDel<Texture2D, TextureDescriptor>? _loadTextureDel;
    private LoadAssetDel<CubeMap, CubeMapDescriptor>? _loadCubeMapDel;
    private LoadAssetDel<Model, MeshDescriptor>? _loadMeshDel;
    private LoadEmbeddedAssetDel<Texture2D, TextureEmbeddedRecord> texDel;
    private LoadEmbeddedAssetDel<MaterialTemplate, MaterialEmbeddedRecord> matDel;


    public void EnsureListCapacity<T>(int capacity) where T : AssetObject =>
        _store!.GetAssetList<T>().EnsureCapacity(capacity);

    public bool IsActive { get; private set; }

    public Shader LoadShader(ShaderDescriptor manifest, bool isCoreAsset) =>
        _store!.Register(manifest, isCoreAsset, out _, _loadShaderDel!);

    public Texture2D LoadTexture2D(TextureDescriptor manifest, bool isCoreAsset) =>
        _store!.Register(manifest, isCoreAsset, out _, _loadTextureDel!);

    public CubeMap LoadCubeMap(CubeMapDescriptor manifest, bool isCoreAsset) =>
        _store!.Register(manifest, isCoreAsset, out _, _loadCubeMapDel!);

    public Model LoadMesh(MeshDescriptor manifest, bool isCoreAsset)
    {
        InvalidOpThrower.ThrowIfAnyNull(_meshLoader, _loadMeshDel);

        var model = _store!.Register(manifest, isCoreAsset, out var embedded, _loadMeshDel!);
        ProcessEmbedded(model.Id, embedded);
        _meshLoader!.ClearState();
        return model;
    }

    public void LoadAllMaterials(MaterialManifest manifest) =>
        _materialLoader!.LoadMaterials(_store!, manifest.Records);


    private void ProcessEmbedded(AssetId assetId, EmbeddedRecord[] embedded)
    {
        if (embedded.Length == 0) return;


        Array.Sort(embedded);
        foreach (var it in embedded)
        {
            InvalidOpThrower.ThrowIfNull(it);
            InvalidOpThrower.ThrowIfNull(it.EmbeddedName);
            InvalidOpThrower.ThrowIf(it.GId == Guid.Empty);
            if (it is TextureEmbeddedRecord tex) _store!.RegisterEmbedded(assetId, tex, texDel);
            if (it is MaterialEmbeddedRecord mat) _store!.RegisterEmbedded(assetId, mat, matDel);
        }
    }


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

        texDel = _textureLoader!.LoadEmbeddedTexture;
        matDel = _materialLoader!.CreateEmbeddedTemplate;
        
        _shaderLoader.Prepare();

        IsActive = true;

        Logger.LogString(LogScope.Assets, "Startup Asset Loader - Activated");
    }

    public void ActivateLazyLoader(AssetStore store, AssetGfxUploader gfx)
    {
        IsActive = true;
        _store = store;
        _gfxUploader = gfx;
        Logger.LogString(LogScope.Assets, "Asset Loader - Activated");
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

        _meshLoader?.Teardown();
        _textureLoader?.Unload();
        _shaderLoader?.Unload();

        _meshLoader = null;
        _textureLoader = null;
        _shaderLoader = null;
        _materialLoader = null;

        _gfxUploader = null;

        IsActive = false;


        Logger.LogString(LogScope.Assets, "Asset Loader - Closed");
    }
}