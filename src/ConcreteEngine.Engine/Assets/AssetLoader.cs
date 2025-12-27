using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Models.Loader;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Diagnostics;

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
    private LoadAdvancedAssetDel<Model, MeshDescriptor>? _loadMeshDel;

    private List<TextureEmbeddedDescriptor>? _embeddedTextures;
    private List<MaterialEmbeddedDescriptor>? _embeddedMaterials;

    private Action<ReadOnlySpan<IAssetEmbeddedDescriptor>>? _enqueueDel;


    public bool IsActive { get; private set; }

    public Shader LoadShader(ShaderDescriptor manifest, bool isCoreAsset) =>
        _store!.RegisterWithFiles(manifest, isCoreAsset, _loadShaderDel!);

    public Texture2D LoadTexture2D(TextureDescriptor manifest, bool isCoreAsset) =>
        _store!.RegisterWithFiles(manifest, isCoreAsset, _loadTextureDel!);

    public CubeMap LoadCubeMap(CubeMapDescriptor manifest, bool isCoreAsset) =>
        _store!.RegisterWithFiles(manifest, isCoreAsset, _loadCubeMapDel!);

    public Model LoadMesh(MeshDescriptor manifest, bool isCoreAsset)
    {
        InvalidOpThrower.ThrowIfAnyNull(_meshLoader, _loadMeshDel, _enqueueDel);

        var model = _store!.RegisterWithEmbedded(manifest, isCoreAsset, _loadMeshDel!, _enqueueDel!);
        if (_embeddedTextures!.Count > 0 || _embeddedMaterials!.Count > 0)
        {
            ProcessEmbedded();
        }

        _meshLoader!.ClearState();
        return model;
    }

    public void LoadAllMaterials(MaterialManifest manifest) =>
        _materialLoader!.LoadMaterials(_store!, manifest.Records);

    private void LoadEmbeddedMaterial(ReadOnlySpan<MaterialEmbeddedDescriptor> materials) =>
        _materialLoader!.LoadEmbeddedMaterials(_store!, materials);

    private void EnqueueEmbedded(ReadOnlySpan<IAssetEmbeddedDescriptor> embedded)
    {
        InvalidOpThrower.ThrowIfNull(_embeddedTextures);
        InvalidOpThrower.ThrowIfNull(_embeddedMaterials);

        foreach (var it in embedded)
        {
            InvalidOpThrower.ThrowIfNull(it);
            InvalidOpThrower.ThrowIf(it.GId == Guid.Empty);
            InvalidOpThrower.ThrowIfNull(it.EmbeddedName);

            if (it is TextureEmbeddedDescriptor tex) _embeddedTextures!.Add(tex);
            if (it is MaterialEmbeddedDescriptor mat) _embeddedMaterials!.Add(mat);
        }
    }

    private void ProcessEmbedded()
    {
        if (_embeddedTextures!.Count > 0)
        {
            LoadEmbeddedAssetDel<Texture2D, TextureEmbeddedDescriptor> del = _textureLoader!.LoadEmbeddedTexture;
            foreach (var it in _embeddedTextures)
            {
                _store!.RegisterEmbedded(it, del);
            }
        }

        if (_embeddedMaterials!.Count > 0)
        {
            LoadEmbeddedMaterial(CollectionsMarshal.AsSpan(_embeddedMaterials));
        }

        _embeddedMaterials.Clear();
        _embeddedTextures.Clear();
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

        _embeddedTextures = new List<TextureEmbeddedDescriptor>(4);
        _embeddedMaterials = new List<MaterialEmbeddedDescriptor>(4);

        _loadShaderDel ??= _shaderLoader.LoadShader;
        _loadTextureDel ??= _textureLoader.LoadTexture2D;
        _loadCubeMapDel ??= _textureLoader.LoadCubeMap;
        _loadMeshDel ??= _meshLoader.LoadModel;

        _enqueueDel = EnqueueEmbedded;

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
        _enqueueDel = null!;

        _embeddedTextures?.Clear();
        _embeddedMaterials?.Clear();
        _embeddedTextures = null!;
        _embeddedMaterials = null!;

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