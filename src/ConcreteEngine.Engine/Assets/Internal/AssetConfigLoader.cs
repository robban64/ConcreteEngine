using System.Text.Json;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Diagnostics;

namespace ConcreteEngine.Engine.Assets.Internal;

internal sealed class AssetConfigLoader
{
    private JsonSerializerOptions? _jsonOptions;

    public AssetManifest LoadAssetManifest()
    {
        _jsonOptions ??= JsonUtility.DefaultJsonOptions;
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

    public T LoadAssetCatalog<T>(string filename) where T : class, IAssetCatalog
    {
        ArgumentNullException.ThrowIfNull(filename);
        _jsonOptions ??= JsonUtility.DefaultJsonOptions;

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

    public void ClearCache() => _jsonOptions = null;
}