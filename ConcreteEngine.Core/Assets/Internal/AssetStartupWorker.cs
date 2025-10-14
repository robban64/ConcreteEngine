#region

using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Assets.Textures;

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
    private Func<IAssetCatalog> _onStartStep = null!;

    public AssetStartupWorker(AssetLoader loader, AssetConfigLoader configLoader, AssetManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(loader, nameof(loader));
        ArgumentNullException.ThrowIfNull(configLoader, nameof(configLoader));
        ArgumentNullException.ThrowIfNull(manifest, nameof(manifest));

        _loader = loader;
        _configLoader = configLoader;
        _manifest = manifest;
    }

    internal void Start(AssetStore store, AssetGfxUploader uploader)
    {
        InvalidOpThrower.ThrowIf(_processOrder != ProcessStepOrder.NotStarted);
        _processOrder = (ProcessStepOrder)1;
        _onStartStep = NextStep;
        _loader.ActivateLoader(store, uploader);
    }

    internal void Finish()
    {
        _loader.DeactivateLoader();
        _currentManifest = null;
        _onStartStep = null!;
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
                ProcessGfxStep(_onStartStep);
                _loader.LoadShader(CurrentManifest<ShaderManifest>().Records[_idx]);
                break;
            case ProcessStepOrder.Textures:
                ProcessGfxStep(_onStartStep);
                _loader.LoadTexture2D(CurrentManifest<TextureManifest>().Records[_idx]);
                break;
            case ProcessStepOrder.CubeMaps:
                ProcessGfxStep(_onStartStep);
                _loader.LoadCubeMap(CurrentManifest<CubeMapManifest>().Records[_idx]);
                break;
            case ProcessStepOrder.Meshes:
                ProcessGfxStep(_onStartStep);
                _loader.LoadMesh(CurrentManifest<MeshManifest>().Records[_idx]);
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


    private bool ProcessGfxStep(Func<IAssetCatalog> onStartStep)
    {
        if (_idx++ == 0) _currentManifest = onStartStep.Invoke();

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
