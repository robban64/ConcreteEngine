#region

using System.Text.Json;
using System.Text.Json.Serialization;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.IO;

#endregion

namespace ConcreteEngine.Core.Assets.Config;

internal sealed class AssetConfigLoader
{
    private readonly JsonSerializerOptions _jsonOptions;

    private readonly string _assetPath;
    private readonly string _manifestFilename;

    public AssetConfigLoader(string assetPath, string manifestFilename)
    {
        _assetPath = assetPath;
        _manifestFilename = manifestFilename;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new Vector2Converter(),
                new Vector3Converter(),
                new Vector4Converter()
            }
        };
    }

    public AssetManifest LoadAssetManifest()
    {
        if (!Directory.Exists(_assetPath))
            throw new DirectoryNotFoundException($"Asset manifest '{_assetPath}' directory not found.");

        var path = Path.Combine(AssetPaths.GetAssetPath(), _manifestFilename);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Manifest '{path}' not found.");

        Console.WriteLine("Loading Asset Manifest...");

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            64 * 1024, FileOptions.SequentialScan);

        var assetManifest = JsonSerializer.Deserialize<AssetManifest>(fs, _jsonOptions) ??
                            throw new InvalidDataException("Invalid manifest.");

        return assetManifest;
    }

    public T LoadManifest<T>(string manifestFilename) where T : class
    {
        ArgumentNullException.ThrowIfNull(manifestFilename, nameof(manifestFilename));

        var path = Path.Combine(AssetPaths.GetAssetPath(), manifestFilename);

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