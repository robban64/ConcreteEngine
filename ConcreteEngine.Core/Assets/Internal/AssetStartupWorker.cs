#region

using System.Diagnostics.CodeAnalysis;
using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Assets.Materials;

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
        Finished
    }

    private const int ProcessOrderCount = 6;

    private ProcessStepOrder _processOrder = ProcessStepOrder.NotStarted;

    private readonly AssetLoader _loader;
    private readonly AssetConfigLoader _configLoader;
    private readonly AssetManifest _manifest;

    private IAssetCatalog? _currentManifest;

    private int _idx = 0;

    private AssetResourceLayout Layout => _manifest.ResourceLayout;
    private Action<ShaderDescriptor> _loadShaderFunc = null!;
    private Action<TextureDescriptor> _loadTextureFunc = null!;
    private Action<CubeMapDescriptor> _loadCubeMapFunc = null!;
    private Action<MeshDescriptor> _loadMeshFunc = null!;

    public AssetStartupWorker(AssetLoader loader, AssetConfigLoader configLoader, AssetManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(loader, nameof(loader));
        ArgumentNullException.ThrowIfNull(configLoader, nameof(configLoader));
        ArgumentNullException.ThrowIfNull(manifest, nameof(manifest));

        _loader = loader;
        _configLoader = configLoader;
        _manifest = manifest;
    }

    [SuppressMessage("ReSharper", "HeapView.DelegateAllocation")]
    internal void Start(AssetStore store, AssetGfxUploader uploader)
    {
        InvalidOpThrower.ThrowIf(_processOrder != ProcessStepOrder.NotStarted);
        _processOrder = (ProcessStepOrder)1;

        _loadShaderFunc = (desc) => _loader.LoadShader(desc);
        _loadTextureFunc = (desc) => _loader.LoadTexture2D(desc);
        _loadCubeMapFunc = (desc) => _loader.LoadCubeMap(desc);
        _loadMeshFunc = (desc) => _loader.LoadMesh(desc);

        _loader.ActivateLoader(store, uploader);
    }

    internal void Finish()
    {
        _loader.DeactivateLoader();
        _currentManifest = null;
        _loadShaderFunc = null!;
        _loadTextureFunc = null!;
        _loadCubeMapFunc = null!;
        _loadMeshFunc = null!;
    }

    public MaterialStore ProcessMaterials()
    {
        var materialManifest = _configLoader.LoadAssetCatalog<MaterialManifest>(Layout.Material);
        var materials = _loader.LoadAllMaterials(materialManifest);
        return new MaterialStore(materials!);
    }

    public bool ProcessGfxAssets()
    {
        switch (_processOrder)
        {
            case ProcessStepOrder.NotStarted:
                throw new InvalidOperationException("Asset loader has not started.");
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
            case ProcessStepOrder.Finished:
                return true;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return false;
    }

    private IAssetCatalog NextStep()
    {
        return _processOrder switch
        {
            ProcessStepOrder.Shaders => _configLoader.LoadAssetCatalog<ShaderManifest>(Layout.Shader),
            ProcessStepOrder.Textures => _configLoader.LoadAssetCatalog<TextureManifest>(Layout.Texture),
            ProcessStepOrder.CubeMaps => _configLoader.LoadAssetCatalog<CubeMapManifest>(Layout.CubeMaps),
            ProcessStepOrder.Meshes => _configLoader.LoadAssetCatalog<MeshManifest>(Layout.Mesh),
            ProcessStepOrder.Finished => _currentManifest,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private T CurrentManifest<T>() where T : class, IAssetCatalog
        => _currentManifest as T ?? throw new InvalidOperationException();

    private IReadOnlyList<TDesc> CurrentRecords<TCatalog, TDesc>()
        where TCatalog : class, IAssetCatalog where TDesc : class, IAssetDescriptor
        => CurrentManifest<TCatalog>().Records as IReadOnlyList<TDesc> ?? throw new InvalidOperationException();


    private bool ProcessManifestStep<TCatalog, TDesc>(Action<TDesc> onStartStep)
        where TCatalog : class, IAssetCatalog where TDesc : class, IAssetDescriptor
    {
        if (_idx == 0) _currentManifest = NextStep();
        onStartStep(CurrentRecords<TCatalog, TDesc>()[_idx++]);

        if (_idx < _currentManifest!.Count) return false;

        _idx = 0;

        var order = (int)_processOrder + 1;
        if (order >= ProcessOrderCount)
        {
            _processOrder = ProcessStepOrder.Finished;
            return false;
        }

        _processOrder = (ProcessStepOrder)order;
        return false;
    }
}