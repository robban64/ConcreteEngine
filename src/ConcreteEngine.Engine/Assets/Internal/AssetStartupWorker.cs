using System.Diagnostics.CodeAnalysis;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;

namespace ConcreteEngine.Engine.Assets.Internal;

internal sealed class AssetStartupWorker
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

    private delegate TAsset AssetLoadModuleDel<out TAsset, in TDesc>(TDesc desc, bool isCore)
        where TAsset : AssetObject where TDesc : class, IAssetDescriptor;

    private AssetLoadModuleDel<Shader, ShaderDescriptor> _loadShaderFunc = null!;
    private AssetLoadModuleDel<Texture2D, TextureDescriptor> _loadTextureFunc = null!;
    private AssetLoadModuleDel<Model, MeshDescriptor> _loadMeshFunc = null!;
    private Action<MaterialManifest> _loadMaterialFunc = null!;

    private readonly AssetLoader _loader;
    private readonly AssetManifestProvider _manifestProvider;

    private IAssetCatalog? _currentManifest;
    private ProcessStepOrder _processOrder = ProcessStepOrder.NotStarted;

    private int _idx;

    public AssetStartupWorker(AssetLoader loader, AssetManifestProvider manifestProvider)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(manifestProvider);

        _loader = loader;
        _manifestProvider = manifestProvider;
    }

    private AssetResourceLayout Layout => _manifestProvider.Manifest.ResourceLayout;


    [SuppressMessage("ReSharper", "HeapView.DelegateAllocation")]
    internal void Start(AssetStore store, AssetGfxUploader uploader)
    {
        InvalidOpThrower.ThrowIf(_processOrder != ProcessStepOrder.NotStarted);
        _processOrder = (ProcessStepOrder)1;

        _loadShaderFunc = (desc, isCore) => _loader.LoadShader(desc, isCore);
        _loadTextureFunc = (desc, isCore) => _loader.LoadTexture2D(desc, isCore);
        _loadMeshFunc = (desc, isCore) => _loader.LoadMesh(desc, isCore);
        _loadMaterialFunc = (desc) => _loader.LoadAllMaterials(desc);

        _loader.ActivateFullLoader(store, uploader);
    }

    internal void Finish()
    {
        _currentManifest = null;
        _loadShaderFunc = null!;
        _loadTextureFunc = null!;
        _loadMeshFunc = null!;
        _loadMaterialFunc = null!;
        _manifestProvider.ClearCache();
    }

    public bool ProcessAssets(int n)
    {
        if (_processOrder == ProcessStepOrder.NotStarted)
            throw new InvalidOperationException("Asset loader has not started.");

        var processSingle = _processOrder is
            ProcessStepOrder.Shaders or
            ProcessStepOrder.Textures or
            ProcessStepOrder.Meshes;


        var length = processSingle ? n : 1;
        for (var i = 0; i < length; i++) Execute();

        return _processOrder == ProcessStepOrder.Finished;
    }


    public bool Execute()
    {
        switch (_processOrder)
        {
            case ProcessStepOrder.NotStarted:
            case ProcessStepOrder.Shaders:
                ProcessManifestStep(_loadShaderFunc);
                break;
            case ProcessStepOrder.Textures:
                ProcessManifestStep(_loadTextureFunc);
                break;
            case ProcessStepOrder.Meshes:
                ProcessManifestStep(_loadMeshFunc);
                break;
            case ProcessStepOrder.Materials:
                ProcessEntireManifest<MaterialTemplate, MaterialManifest>(_loadMaterialFunc);
                break;
            case ProcessStepOrder.Finished:
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(_processOrder));
        }

        return false;
    }


    private T CurrentManifest<T>() where T : class, IAssetCatalog => (T)_currentManifest!;

    private IReadOnlyList<TDesc> CurrentRecords<TDesc>() where TDesc : class, IAssetDescriptor =>
        (IReadOnlyList<TDesc>)_currentManifest!.Records;


    private void ProcessManifestStep<TAsset, TDesc>(AssetLoadModuleDel<TAsset, TDesc> onStartStep)
        where TAsset : AssetObject where TDesc : class, IAssetDescriptor
    {
        if (_idx == 0)
        {
            _currentManifest = NextManifest();
            _loader.EnsureListCapacity<TAsset>(_currentManifest.Count);
        }

        var records = CurrentRecords<TDesc>();
        if (records.Count == 0)
        {
            NextStepOrder();
            return;
        }

        onStartStep(CurrentRecords<TDesc>()[_idx++], false);
        if (_idx < _currentManifest!.Count) return;
        NextStepOrder();
    }

    private void ProcessEntireManifest<TAsset, TCatalog>(Action<TCatalog> onStart) where TCatalog : class, IAssetCatalog
        where TAsset : AssetObject
    {
        _currentManifest = NextManifest();
        _loader.EnsureListCapacity<TAsset>(_currentManifest.Count);

        onStart(CurrentManifest<TCatalog>());
        NextStepOrder();
    }

    private IAssetCatalog NextManifest()
    {
        return (_processOrder switch
        {
            ProcessStepOrder.Shaders => CoreShaderManifest.GetManifest,
            ProcessStepOrder.Textures => _manifestProvider.TextureManifest,
            ProcessStepOrder.Meshes => _manifestProvider.ModelManifest,
            ProcessStepOrder.Materials => _manifestProvider.MaterialManifest,
            ProcessStepOrder.Finished => _currentManifest,
            _ => throw new ArgumentOutOfRangeException(nameof(_processOrder))
        })!;
    }

    private void NextStepOrder()
    {
        _idx = 0;
        var order = (int)_processOrder + 1;
        _processOrder = order >= (int)ProcessStepOrder.Finished ? ProcessStepOrder.Finished : (ProcessStepOrder)order;
    }
}