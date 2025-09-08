#region

using System.Text.Json;
using System.Text.Json.Serialization;
using ConcreteEngine.Core.Assets.IO;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Assets;

public interface IAssetSystem : IGameEngineSystem
{
    bool TryGet<T>(string name, out T resource) where T : class, IAssetFile;
    T Get<T>(string name) where T : class, IAssetFile;
    List<T> GetAll<T>() where T : class, IAssetFile;
}

public sealed class AssetSystem : IAssetSystem
{
    private readonly record struct AssetKey(Type RegistryType, string Name)
    {
        public static AssetKey For<T>(string name) where T : IAssetFile => new(typeof(T), name);
    }

    private static bool _initialized = false;

    private readonly Dictionary<AssetKey, IAssetFile> _store = new(32);

    private readonly string _assetPath;
    private readonly string _manifestFilename;

    private MaterialStore _materialStore = null!;

    public MaterialStore MaterialStore => _materialStore;

    private readonly JsonSerializerOptions _jsonOptions;

    private string BasePath => Path.Combine(Directory.GetCurrentDirectory(), _assetPath);

    internal AssetSystem(
        string assetPath = "assets",
        string manifestFilename = "manifest.json")
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

    internal AssetLoader CreateLoader()
    {
        return new AssetLoader(_assetPath);
    }

    internal void FinishLoading(AssetLoader loader)
    {
        var data = loader.GetData();
        LoadResources(data.Shaders);
        LoadResources(data.Meshes);
        LoadResources(data.Textures);
        LoadResources(data.CubeMaps);
        
        var materials = LoadMaterialStore("materials.json");
        _materialStore = new MaterialStore(materials);
    }


    public bool TryGet<T>(string name, out T resource) where T : class, IAssetFile
    {
        var key = AssetKey.For<T>(name);
        if (_store.TryGetValue(key, out var asset) && asset is T typed)
        {
            resource = typed;
            return true;
        }

        resource = null!;
        return false;
    }

    public T Get<T>(string name) where T : class, IAssetFile
    {
        var key = AssetKey.For<T>(name);

        if (_store.TryGetValue(key, out var asset) && asset is T typed)
            return typed;

        throw new InvalidCastException($"Asset '{name}' not found or incorrect type.");
    }

    public List<T> GetAll<T>() where T : class, IAssetFile
    {
        var result = new List<T>(8);
        foreach (var (name, asset) in _store)
        {
            if (asset is T typedAsset) result.Add(typedAsset);
        }

        return result;
    }

    public void Shutdown()
    {
        foreach (var asset in _store.Values)
            if (asset is IDisposable disposable)
                disposable.Dispose();
    }

    internal AssetRecordResult LoadManifest()
    {
        if (!Directory.Exists(_assetPath))
        {
            throw new DirectoryNotFoundException($"Asset manifest '{_assetPath}' directory not found.");
        }

        AssetPaths.AssetPath = _assetPath;

        var manifestPath = Path.Combine(BasePath, _manifestFilename);
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException($"Manifest '{manifestPath}' not found.");

        Console.WriteLine("Loading Asset Manifest...");

        var json = File.ReadAllText(manifestPath);
        var assetManifest = JsonSerializer.Deserialize<AssetManifest>(json, _jsonOptions) ??
                            throw new InvalidDataException("Invalid manifest.");

        var m = assetManifest.ResourceManifest;
        return new AssetRecordResult
        {
            Textures = LoadAllEntries<TextureManifestRecord>(m.Texture, true),
            Shaders = LoadAllEntries<ShaderManifestRecord>(m.Shader, true),
            Meshes = LoadAllEntries<MeshManifestRecord>(m.Mesh, false),
            Cubemaps = LoadAllEntries<CubeMapManifestRecord>(m.CubeMaps ?? "", false)
        };
    }


    private void LoadResources<T>(IReadOnlyList<T> resources) where T : class, IGraphicAssetFile
    {
        foreach (var resource in resources)
        {
            if(!_store.TryAdd(AssetKey.For<T>(resource.Name), resource))
                throw new InvalidOperationException($"Asset '{resource.Name}' is already exists.");
        }
    }

    private IReadOnlyList<T> LoadAllEntries<T>(string manifestFilename, bool required) where T : IAssetManifestRecord
    {
        ArgumentNullException.ThrowIfNull(manifestFilename, nameof(manifestFilename));

        var path = Path.Combine(BasePath, manifestFilename);
        var exists = File.Exists(path);
        if (!exists && !required) return [];
        if (!exists && required)
        {
            throw new FileNotFoundException(
                $"Resource manifest {typeof(T).Name} with path {path} does not exists.");
        }


        var json = File.ReadAllText(path);
        var manifest = JsonSerializer.Deserialize<AssetResourceManifest<T>>(json, _jsonOptions) ??
                       throw new InvalidDataException($"Invalid resource manifest for {typeof(T).Name}.");

        if (manifest.Resources == null)
            throw new InvalidDataException($"{typeof(T).Name} manifest have null resources.");

        Console.WriteLine($"Loading Assets - ({typeof(T).Name})");

        return manifest.Resources;
    }
    
    /*
public void Remove<T>(T assetFile) where T : class, IAssetFile
{
    if (assetFile is IGraphicAssetFile graphicsResource)
    {
        if(!graphicsResource.GraphicsResource.IsDisposed)
            graphics.DisposeResource(graphicsResource.GraphicsResource);
    }

    _store.Remove(assetFile.Name);
}
*/

   
    private IReadOnlyList<MaterialTemplate> LoadMaterialStore(string manifestFilename)
    {
        var entries = LoadAllEntries<MaterialManifestRecord>(manifestFilename, false);
        if(entries.Count == 0) return [];

        var result = new List<MaterialTemplate>();

        foreach (var entry in entries)
        {
            var mat = MaterialHandler(entry);
            if(!_store.TryAdd(AssetKey.For<MaterialTemplate>(mat.Name), mat))
                throw new InvalidOperationException($"Asset '{mat.Name}' is already exists.");
            
            result.Add(mat);
        }
        
        _materialStore = new MaterialStore(result);
        return result;

        MaterialTemplate MaterialHandler(MaterialManifestRecord record)
        {
            Texture2D[] textures = [];
            CubeMap? cubeMap = null;
            if (record.Cubemap != null)
            {
                cubeMap = Get<CubeMap>(record.Cubemap);
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
                CubeMap = cubeMap,
            };

        }
    }
  
}