#region

using System.Diagnostics.CodeAnalysis;
using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets.Descriptors;

#endregion

namespace ConcreteEngine.Core.Assets.Internal;

internal sealed class AssetStartupWorker
{
    internal enum ProcessStepOrder
    {
        NotStarted,
        Shaders,
        Textures,
        CubeMaps,
        Meshes,
        Materials,
        Finished
    }

    private Action<ShaderDescriptor> _loadShaderFunc = null!;
    private Action<TextureDescriptor> _loadTextureFunc = null!;
    private Action<CubeMapDescriptor> _loadCubeMapFunc = null!;
    private Action<MeshDescriptor> _loadMeshFunc = null!;
    private Action<MaterialManifest> _loadMaterialFunc = null!;

    private readonly AssetLoader _loader;
    private readonly AssetConfigLoader _configLoader;
    private readonly AssetManifest _manifest;

    private int _idx = 0;
    private IAssetCatalog? _currentManifest;
    private ProcessStepOrder _processOrder = ProcessStepOrder.NotStarted;


    public AssetStartupWorker(AssetLoader loader, AssetConfigLoader configLoader, AssetManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(loader, nameof(loader));
        ArgumentNullException.ThrowIfNull(configLoader, nameof(configLoader));
        ArgumentNullException.ThrowIfNull(manifest, nameof(manifest));

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

        _loadShaderFunc = (desc) => _loader.LoadShader(desc);
        _loadTextureFunc = (desc) => _loader.LoadTexture2D(desc);
        _loadCubeMapFunc = (desc) => _loader.LoadCubeMap(desc);
        _loadMeshFunc = (desc) => _loader.LoadMesh(desc);
        _loadMaterialFunc = (desc) => _loader.LoadAllMaterials(desc);

        _loader.ActivateLoader(store, uploader);
    }

    internal void Finish()
    {
        _currentManifest = null;
        _loadShaderFunc = null!;
        _loadTextureFunc = null!;
        _loadCubeMapFunc = null!;
        _loadMeshFunc = null!;
        _loadMaterialFunc = null!;
    }

    public bool ProcessAssets(int n)
    {
        if (_processOrder == ProcessStepOrder.NotStarted)
            throw new InvalidOperationException("Asset loader has not started.");

        var order = (int)_processOrder;

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
                ProcessManifestStep<ShaderManifest, ShaderDescriptor>(_loadShaderFunc);
                break;
            case ProcessStepOrder.Textures:
                ProcessManifestStep<TextureManifest, TextureDescriptor>(_loadTextureFunc);
                break;
            case ProcessStepOrder.CubeMaps:
                ProcessManifestStep<CubeMapManifest, CubeMapDescriptor>(_loadCubeMapFunc);
                break;
            case ProcessStepOrder.Meshes:
                ProcessManifestStep<MeshManifest, MeshDescriptor>(_loadMeshFunc);
                break;
            case ProcessStepOrder.Materials:
                ProcessEntireManifest(_loadMaterialFunc);
                break;
            case ProcessStepOrder.Finished:
                return true;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return false;
    }


    private T CurrentManifest<T>() where T : class, IAssetCatalog
        => _currentManifest as T ?? throw new InvalidOperationException();

    private IReadOnlyList<TDesc> CurrentRecords<TCatalog, TDesc>()
        where TCatalog : class, IAssetCatalog where TDesc : class, IAssetDescriptor
        => CurrentManifest<TCatalog>().Records as IReadOnlyList<TDesc> ?? throw new InvalidOperationException();


    private void ProcessManifestStep<TCatalog, TDesc>(Action<TDesc> onStartStep)
        where TCatalog : class, IAssetCatalog where TDesc : class, IAssetDescriptor
    {
        if (_idx == 0) _currentManifest = NextManifest();
        onStartStep(CurrentRecords<TCatalog, TDesc>()[_idx++]);

        if (_idx < _currentManifest!.Count) return;
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
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void NextStepOrder()
    {
        _idx = 0;
        var order = (int)_processOrder + 1;
        _processOrder = order >= (int)ProcessStepOrder.Finished ? ProcessStepOrder.Finished : (ProcessStepOrder)order;
    }
}