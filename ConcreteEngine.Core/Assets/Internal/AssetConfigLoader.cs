#region

using System.Text.Json;
using System.Text.Json.Serialization;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Assets.IO;

#endregion

namespace ConcreteEngine.Core.Assets.Internal;

internal sealed class AssetConfigLoader
{
    private readonly JsonSerializerOptions _jsonOptions;

    internal AssetConfigLoader()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(),
                new Vector2Converter(),
                new Vector3Converter(),
                new Vector4Converter()
            }
        };
    }

    public AssetManifest LoadAssetManifest()
    {
        if (!Directory.Exists(AssetPaths.AssetFolder))
            throw new DirectoryNotFoundException($"Asset '{AssetPaths.AssetFolder}' directory not found.");

        var path = AssetPaths.GetManifestPath();

        if (!File.Exists(path))
            throw new FileNotFoundException($"Manifest '{path}' not found.");

        Console.WriteLine("Loading Asset Manifest...");

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            64 * 1024, FileOptions.SequentialScan);

        var assetManifest = JsonSerializer.Deserialize<AssetManifest>(fs, _jsonOptions) ??
                            throw new InvalidDataException("Invalid manifest.");

        return assetManifest;
    }

    public T LoadAssetCatalog<T>(string filename) where T : class, IAssetCatalog
    {
        ArgumentNullException.ThrowIfNull(filename, nameof(filename));

        var path = AssetPaths.GetAssetSubPath(filename);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Resource manifest {typeof(T).Name} with path {path} does not exists.");


        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            64 * 1024, FileOptions.SequentialScan);

        var manifest = JsonSerializer.Deserialize<T>(fs, _jsonOptions)
                       ?? throw new InvalidDataException($"Invalid resource manifest for {typeof(T).Name}.");

        Console.WriteLine($"Loading Assets - ({typeof(T).Name})");

        return manifest;
    }
}