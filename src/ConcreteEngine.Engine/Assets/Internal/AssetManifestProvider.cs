using System.Text.Json;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Diagnostics;

namespace ConcreteEngine.Engine.Assets.Internal;

internal sealed class AssetManifestProvider
{
    private static JsonSerializerOptions? _jsonOptions;
    
    public AssetManifest Manifest = null!;
    
    public ShaderManifest ShaderManifest = null!;
    public TextureManifest TextureManifest = null!;
    public CubeMapManifest CubeManifest = null!;
    public MeshManifest ModelManifest = null!;
    public MaterialManifest MaterialManifest = null!;

    public IAssetCatalog[] ManifestCatalog = [];

    public void LoadManifest()
    {
        _jsonOptions ??= JsonUtility.DefaultJsonOptions;

        Manifest = LoadAssetManifest();
        
        var layout = Manifest.ResourceLayout;
        ShaderManifest = LoadAssetCatalog<ShaderManifest>(layout.Shader);
        TextureManifest = LoadAssetCatalog<TextureManifest>(layout.Texture);
        CubeManifest = LoadAssetCatalog<CubeMapManifest>(layout.CubeMaps!);
        ModelManifest = LoadAssetCatalog<MeshManifest>(layout.Mesh);
        MaterialManifest = LoadAssetCatalog<MaterialManifest>(layout.Material);
        
        ManifestCatalog = [ShaderManifest, TextureManifest, CubeManifest, ModelManifest, MaterialManifest];
    }
    
    public void ClearCache()
    {
        Manifest = null!;
        ShaderManifest = null!;
        TextureManifest = null!;
        CubeManifest = null!;
        ModelManifest = null!;
        MaterialManifest = null!;
        _jsonOptions = null;
    }

    private static AssetManifest LoadAssetManifest()
    {
        Logger.LogString(LogScope.Assets, "Loading Asset Manifest...");

        if (!Directory.Exists(EnginePath.AssetRoot))
            throw new DirectoryNotFoundException($"Asset '{EnginePath.AssetRoot}' directory not found.");

        var path = Path.Combine(EnginePath.AssetRoot, EnginePath.ManifestFilename);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Manifest '{path}' not found.");

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            64 * 1024, FileOptions.SequentialScan);

        var assetManifest = JsonSerializer.Deserialize<AssetManifest>(fs, _jsonOptions) ??
                            throw new InvalidDataException("Invalid manifest.");

        return assetManifest;
    }

    private static T LoadAssetCatalog<T>(string filename) where T : class, IAssetCatalog
    {
        ArgumentNullException.ThrowIfNull(filename);

        var path = Path.Combine(EnginePath.AssetRoot, filename);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Resource manifest {typeof(T).Name} with path {path} does not exists.");


        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            64 * 1024, FileOptions.SequentialScan);

        var manifest = JsonSerializer.Deserialize<T>(fs, _jsonOptions)
                       ?? throw new InvalidDataException($"Invalid resource manifest for {typeof(T).Name}.");

        Logger.LogString(LogScope.Assets, $"Loading Assets - ({typeof(T).Name})");

        return manifest;
    }

}