#region

#endregion


using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets.Config;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetProcessor
{
    internal enum ProcessOrder
    {
        NotStarted,
        Shaders,
        Textures,
        CubeMaps,
        Meshes,

        //Materials,
        Finished
    }

    private const int ProcessOrderCount = 6;

    private ProcessOrder _processOrder = ProcessOrder.NotStarted;

    private readonly AssetLoader _loader;
    private readonly AssetConfigLoader _configLoader;
    private readonly AssetManifest _manifest;

    private ShaderManifest? _shaderManifest;
    private TextureManifest? _textureManifest;
    private CubeMapManifest? _cubeMapManifest;
    private MeshManifest? _meshManifest;


    private int _idx = 0;

    private AssetResourceLayout Layout => _manifest.ResourceLayout;

    public AssetProcessor(AssetLoader loader, AssetConfigLoader configLoader, AssetManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(loader);
        _loader = loader;
        _configLoader = configLoader;
        _manifest = manifest;
    }

    internal void Start(AssetStore store, AssetGfxUploader uploader)
    {
        InvalidOpThrower.ThrowIf(_processOrder != ProcessOrder.NotStarted);
        _processOrder = (ProcessOrder)1;
        _loader.ActivateLoader(store, uploader);
        
        _shaderManifest = _configLoader.LoadManifest<ShaderManifest>(Layout.Shader);

    }

    internal void Finish()
    {
        _loader.DeactivateLoader();
    }

    public bool Process()
    {
        switch (_processOrder)
        {
            case ProcessOrder.NotStarted:
                throw new InvalidOperationException("Asset loader has not started.");
            case ProcessOrder.Shaders:
                _loader.LoadShader(_shaderManifest!.Records[_idx]);
                if (ProcessStep(_shaderManifest.Records.Length))
                {
                    _textureManifest = _configLoader.LoadManifest<TextureManifest>(Layout.Texture);
                    _shaderManifest = null;
                }

                break;
            case ProcessOrder.Textures:
                _loader.LoadTexture2D(_textureManifest!.Records[_idx]);
                if (ProcessStep(_textureManifest.Records.Length))
                {
                    if (Layout.CubeMaps is null) _processOrder++;
                    else _cubeMapManifest = _configLoader.LoadManifest<CubeMapManifest>(Layout.CubeMaps);

                    _textureManifest = null;
                }

                break;
            case ProcessOrder.CubeMaps:
                _loader.LoadCubeMap(_cubeMapManifest!.Records[_idx]);
                if (ProcessStep(_cubeMapManifest.Records.Length))
                {
                    _meshManifest = _configLoader.LoadManifest<MeshManifest>(Layout.Mesh);
                    _cubeMapManifest = null;
                }

                break;
            case ProcessOrder.Meshes:
                _loader.LoadMesh(_meshManifest!.Records[_idx]);
                if (ProcessStep(_meshManifest.Records.Length))
                    _meshManifest = null;
                break;
            case ProcessOrder.Finished:
                return true;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return false;
    }

    private bool ProcessStep(int length, Action? step = null)
    {
        _idx++;
        if (_idx < length) return false;
        _idx = 0;
        var order = (int)_processOrder + 1;
        if (order >= ProcessOrderCount)
        {
            _processOrder = ProcessOrder.Finished;
            return false;
        }

        _processOrder = (ProcessOrder)order;
        return true;
    }
}