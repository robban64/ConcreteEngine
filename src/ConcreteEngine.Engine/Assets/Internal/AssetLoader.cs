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

internal static class AssetPriority 
{
    public const int Texture = 0;
    public const int Material = 10;
    public const int Mesh = 20;
}

internal sealed class AssetLoader
{
    private AssetStore? _store;
    private AssetDataProvider? _dataProvider;

    private AssetGfxUploader? _gfxUploader;

    private TextureLoaderModule? _textureLoader;
    private ModelLoaderModule? _meshLoader;
    private ShaderLoaderModule? _shaderLoader;
    private MaterialLoader? _materialLoader;

    public bool IsActive { get; private set; }

    public void EnsureListCapacity<T>(int capacity) where T : AssetObject =>
        _store!.GetAssetList<T>().EnsureCapacity(capacity);


    public void LoadShader(ShaderRecord record)
    {
        
    }

    public void LoadTexture2D(TextureRecord manifest)
    {
        
    }
    
    public void LoadModel(ModelRecord manifest)
    {
 
    }

    public void LoadMaterial(MaterialRecord record)
    {
        
    }

    private void ProcessEmbedded(AssetId assetId, EmbeddedRecord[] embedded)
    {
        if (embedded.Length == 0) return;


        Array.Sort(embedded);
        foreach (var it in embedded)
        {
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