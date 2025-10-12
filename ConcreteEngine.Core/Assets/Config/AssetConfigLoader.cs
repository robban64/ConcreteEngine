using System.Text.Json;
using System.Text.Json.Serialization;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.IO;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Assets.Textures;

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

    /*
    private IReadOnlyList<MaterialTemplate>? LoadMaterialStore(string manifestFilename)
    {
        var entries =
            LoadAllEntries<MaterialManifestRecord>(manifestFilename, true);

        ArgumentNullException.ThrowIfNull(entries, nameof(entries));

        if (entries.Resources.Length == 0) return null;

        var result = new List<MaterialTemplate>();

        var resources = entries.Resources;
        foreach (var entry in resources)
        {
            var mat = MaterialHandler(entry);
            if (!_store.TryAdd(AssetKey.For<MaterialTemplate>(mat.Name), mat))
                throw new InvalidOperationException($"Asset '{mat.Name}' is already exists.");

            result.Add(mat);
        }

        foreach (var mat in result)
            mat.Initialize();

        _materialStore = new MaterialStore(result);
        return result;

        MaterialTemplate MaterialHandler(MaterialManifestRecord record)
        {
            Texture2D[] textures = [];
            CubeMap? cubeMap = null;
            if (record.CubeMap != null)
            {
                cubeMap = Get<CubeMap>(record.CubeMap);
            }
            else if (record.Textures != null)
            {
                textures = new Texture2D[record.Textures.Length];
                for (var i = 0; i < record.Textures.Length; i++)
                {
                    textures[i] = Get<Texture2D>(record.Textures[i]);
                }
            }

            var shader = Get<Shader>(record.Shader);

            return new MaterialTemplate
            {
                Name = record.Name,
                Shader = shader,
                Color = record.Color,
                Textures = textures,
                CubeMap = cubeMap
            };
        }
    }*/
}