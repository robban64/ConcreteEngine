using System.Diagnostics.CodeAnalysis;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Engine.Assets.Descriptors;
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
        CubeMaps,
        Meshes,
        Materials,
        Finished
    }

    private delegate TAsset AssetLoadModuleDel<out TAsset, in TDesc>(TDesc desc, bool isCore)
        where TAsset : AssetObject where TDesc : class, IAssetDescriptor;

    private AssetLoadModuleDel<Shader, ShaderDescriptor> _loadShaderFunc = null!;
    private AssetLoadModuleDel<Texture2D, TextureDescriptor> _loadTextureFunc = null!;
    private AssetLoadModuleDel<CubeMap, CubeMapDescriptor> _loadCubeMapFunc = null!;
    private AssetLoadModuleDel<Model, MeshDescriptor> _loadMeshFunc = null!;
    private Action<MaterialManifest> _loadMaterialFunc = null!;

    private readonly AssetLoader _loader;
    private readonly AssetConfigLoader _configLoader;
    private readonly AssetManifest _manifest;

    private IAssetCatalog? _currentManifest;
    private ProcessStepOrder _processOrder = ProcessStepOrder.NotStarted;

    private int _idx;

    public AssetStartupWorker(AssetLoader loader, AssetConfigLoader configLoader, AssetManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(configLoader);
        ArgumentNullException.ThrowIfNull(manifest);

        _loader = loader;
        _configLoader = configLoader;
        _manifest = manifest;
    }

    private AssetResourceLayout Layout => _manifest.ResourceLayout;


    [SuppressMessage("ReSharper", "HeapView.DelegateAllocation")]
    internal void Start(AssetStore store, AssetGfxUploader uploader)
    {
        InvalidOpThrower.ThrowIf(_processOrder != ProcessStepOrder.NotStarted);
        _processOrder = (ProcessStepOrder)1;

        _loadShaderFunc = (desc, isCore) => _loader.LoadShader(desc, isCore);
        _loadTextureFunc = (desc, isCore) => _loader.LoadTexture2D(desc, isCore);
        _loadCubeMapFunc = (desc, isCore) => _loader.LoadCubeMap(desc, isCore);
        _loadMeshFunc = (desc, isCore) => _loader.LoadMesh(desc, isCore);
        _loadMaterialFunc = (desc) => _loader.LoadAllMaterials(desc);

        _loader.ActivateFullLoader(store, uploader);
    }

    internal void Finish()
    {
        _currentManifest = null;
        _loadShaderFunc = null!;
        _loadTextureFunc = null!;
        _loadCubeMapFunc = null!;
        _loadMeshFunc = null!;
        _loadMaterialFunc = null!;
        _configLoader.ClearCache();
    }

    public bool ProcessAssets(int n)
    {
        if (_processOrder == ProcessStepOrder.NotStarted)
            throw new InvalidOperationException("Asset loader has not started.");

        var processSingle = _processOrder is
            ProcessStepOrder.Shaders or
            ProcessStepOrder.Textures or
            ProcessStepOrder.CubeMaps or
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
                ProcessManifestStep(_loadShaderFunc, CoreShaderManifest.GetManifest.Records);
                break;
            case ProcessStepOrder.Textures:
                ProcessManifestStep(_loadTextureFunc);
                break;
            case ProcessStepOrder.CubeMaps:
                ProcessManifestStep(_loadCubeMapFunc);
                break;
            case ProcessStepOrder.Meshes:
                ProcessManifestStep(_loadMeshFunc);
                break;
            case ProcessStepOrder.Materials:
                ProcessEntireManifest(_loadMaterialFunc);
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


    private void ProcessManifestStep<TAsset, TDesc>(AssetLoadModuleDel<TAsset, TDesc> onStartStep,
        TDesc[]? coreManifest = null)
        where TAsset : AssetObject where TDesc : class, IAssetDescriptor
    {
        if (_idx == 0) _currentManifest = NextManifest();
        var coreLength = coreManifest?.Length ?? 0;
        if (coreLength > 0 && _idx < coreLength)
        {
            onStartStep(coreManifest![_idx++], true);
            return;
        }

        var records = CurrentRecords<TDesc>();
        if (records.Count == 0)
        {
            NextStepOrder();
            return;
        }

        onStartStep(CurrentRecords<TDesc>()[_idx++], false);
        if (_idx < _currentManifest!.Count + coreLength) return;
        NextStepOrder();
    }

    private void ProcessEntireManifest<TCatalog>(Action<TCatalog> onStart) where TCatalog : class, IAssetCatalog
    {
        _currentManifest = NextManifest();
        onStart(CurrentManifest<TCatalog>());
        NextStepOrder();
    }

    private IAssetCatalog NextManifest()
    {
        return _processOrder switch
        {
            ProcessStepOrder.Shaders => _configLoader.LoadAssetCatalog<ShaderManifest>(Layout.Shader),
            ProcessStepOrder.Textures => _configLoader.LoadAssetCatalog<TextureManifest>(Layout.Texture),
            ProcessStepOrder.CubeMaps => _configLoader.LoadAssetCatalog<CubeMapManifest>(Layout.CubeMaps),
            ProcessStepOrder.Meshes => _configLoader.LoadAssetCatalog<MeshManifest>(Layout.Mesh),
            ProcessStepOrder.Materials => _configLoader.LoadAssetCatalog<MaterialManifest>(Layout.Material),
            ProcessStepOrder.Finished => _currentManifest,
            _ => throw new ArgumentOutOfRangeException(nameof(_processOrder))
        };
    }

    private void NextStepOrder()
    {
        _idx = 0;
        var order = (int)_processOrder + 1;
        _processOrder = order >= (int)ProcessStepOrder.Finished ? ProcessStepOrder.Finished : (ProcessStepOrder)order;
    }
}