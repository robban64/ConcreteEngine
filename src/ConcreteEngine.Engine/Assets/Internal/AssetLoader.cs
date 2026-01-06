using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Models.Loader;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Configuration.IO;
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
    private enum ProcessStepOrder
    {
        NotStarted,
        Shaders,
        Textures,
        Meshes,
        Materials,
        Finished
    }

    private AssetStore? _store;
    private AssetDataProvider? _dataProvider;

    private AssetGfxUploader? _gfxUploader;

    private TextureLoaderModule? _textureLoader;
    private ModelLoaderModule? _meshLoader;
    private ShaderLoaderModule? _shaderLoader;
    private MaterialLoader? _materialLoader;

    private Queue<AssetRecord>[] _recordQueue;

    private ProcessStepOrder _step;

    public bool IsActive { get; private set; }

    public void EnsureListCapacity<T>(int capacity) where T : AssetObject =>
        _store!.GetAssetList<T>().EnsureCapacity(capacity);

    public void StartLoader(Queue<AssetRecord>[] recordQueue)
    {
        _recordQueue = recordQueue;
    }

    public void LoadSetup()
    {
        switch (_step)
        {
            case ProcessStepOrder.NotStarted: _step = ProcessStepOrder.Shaders; break;
            case ProcessStepOrder.Shaders:
                LoadShaders(_recordQueue[AssetEnums.ToAssetIndex<Shader>()]);
                break;
            case ProcessStepOrder.Textures:
                LoadTextures(_recordQueue[AssetEnums.ToAssetIndex<Texture2D>()]);
                break;
            case ProcessStepOrder.Meshes:
                LoadModel(_recordQueue[AssetEnums.ToAssetIndex<Model>()]);
                break;
            case ProcessStepOrder.Materials:
                LoadMaterial(_recordQueue[AssetEnums.ToAssetIndex<MaterialTemplate>()]);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private LoaderContext MakeContext(Guid gid, string path, bool isHotReload)
    {
        _store!.TryGetIdByGuid(gid, out var assetId);
        return new LoaderContext { Id = assetId, GId = gid, IsHotReload = isHotReload, FilePath = path };
    }

    public void LoadShaders(Queue<AssetRecord> queue)
    {
        while (queue.TryDequeue(out var record))
            _shaderLoader!.LoadAsset((ShaderRecord)record, MakeContext(record.GId, EnginePath.ShaderPath, false));

        _step = ProcessStepOrder.Textures;
    }

    public void LoadTextures(Queue<AssetRecord> queue)
    {
        int n = 6;
        while (queue.TryDequeue(out var record))
            _textureLoader!.LoadAsset((TextureRecord)record, MakeContext(record.GId, EnginePath.TexturePath, false));

        if (queue.Count == 0) _step = ProcessStepOrder.Meshes;
    }

    public void LoadModel(Queue<AssetRecord> queue)
    {
        int n = 6;
        while (queue.TryDequeue(out var record))
            _meshLoader!.LoadAsset((ModelRecord)record, MakeContext(record.GId, EnginePath.MeshPath, false));

        if (queue.Count == 0) _step = ProcessStepOrder.Materials;
    }

    public void LoadMaterial(Queue<AssetRecord> queue)
    {
        while (queue.TryDequeue(out var record))
            _materialLoader!.LoadAsset((MaterialRecord)record, MakeContext(record.GId, EnginePath.MaterialPath, false));

        _step = ProcessStepOrder.Finished;
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