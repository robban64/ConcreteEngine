#region

using System.Diagnostics;
using System.Text.Json;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Engine.Assets.Internal;

internal sealed class AssetConfigLoader
{
    private JsonSerializerOptions? _jsonOptions;

    public AssetManifest LoadAssetManifest()
    {
        _jsonOptions ??= JsonUtility.DefaultJsonOptions;
        Logger.LogString(LogScope.Assets, "Loading Asset Manifest...");

        if (!Directory.Exists(AssetPaths.AssetRoot))
            throw new DirectoryNotFoundException($"Asset '{AssetPaths.AssetRoot}' directory not found.");

        var path = Path.Combine(AssetPaths.AssetRoot, AssetPaths.ManifestFilename);

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

        var path = Path.Combine(AssetPaths.AssetRoot, filename);

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