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
        public static AssetKey For<T>(string name) => new(typeof(T), name);
    }
    
    private readonly Dictionary<AssetKey, IAssetFile> _store = new(64);
    private readonly IGraphicsDevice _graphics;
    private readonly string _assetPath;
    private readonly string _manifestFilename;

    private MaterialStore _materialStore = null!;
    private static bool _initialized = false;

    public MaterialStore MaterialStore => _materialStore;

    private readonly JsonSerializerOptions _jsonOptions;

    internal AssetSystem(IGraphicsDevice graphics,
        string assetPath = "assets",
        string manifestFilename = "manifest.json")
    {
        _graphics = graphics;
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
                new Vector4Converter(),
                new MaterialValueConverter()
            }
        };
    }

    private string BasePath => Path.Combine(Directory.GetCurrentDirectory(), _assetPath);

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

    public void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException($"{nameof(AssetSystem)} is already initialized");

        LoadFromManifest();
        _initialized = true;
    }

    public void Shutdown()
    {
        foreach (var asset in _store.Values)
            if (asset is IDisposable disposable)
                disposable.Dispose();
    }

    private void LoadFromManifest()
    {
        if (!Directory.Exists(_assetPath))
        {
            throw new DirectoryNotFoundException($"Asset manifest '{_assetPath}' directory not found.");
        }

        var manifestPath = Path.Combine(BasePath, _manifestFilename);
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException($"Manifest '{manifestPath}' not found.");

        Console.WriteLine("Loading Asset Manifest...");

        var json = File.ReadAllText(manifestPath);
        var assetManifest = JsonSerializer.Deserialize<AssetManifest>(json, _jsonOptions) ??
                            throw new InvalidDataException("Invalid manifest.");

        var resourceManifest = assetManifest.ResourceManifest;
        var loader = new AssetLoader(_graphics, _assetPath);

        // Texture2D
        LoadEntries<AssetTextureRecord, Texture2D>(resourceManifest.Texture, loader.LoadTexture2D);

        // Shader
        LoadEntries<AssetShaderRecord, Shader>(resourceManifest.Shader, loader.LoadShader);
        
        // Mesh
        LoadEntries<AssetMeshRecord, Mesh>(resourceManifest.Mesh, loader.LoadMesh);

        if (resourceManifest.CubeMaps != null)
        {
            LoadEntries<AssetCubeMapRecord, CubeMap>(resourceManifest.CubeMaps, loader.LoadCubeMap);
        }

        // Material
        LoadMaterialStore(resourceManifest.Material, loader);

        loader.ClearCache();
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

    private void LoadEntries<TRecord, TResult>(string manifestFilename, Func<TRecord, TResult> loader,
        Action<TResult>? onAdd = null)
        where TRecord : IAssetManifestRecord where TResult : class, IAssetFile
    {
        var path = Path.Combine(BasePath, manifestFilename);
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Resource manifest {typeof(TRecord).Name} with path {path} does not exists.");

        var json = File.ReadAllText(path);
        var manifest = JsonSerializer.Deserialize<AssetResourceManifest<TRecord>>(json, _jsonOptions) ??
                       throw new InvalidDataException($"Invalid resource manifest for {typeof(TRecord).Name}.");

        if (manifest.Resources == null)
            throw new InvalidDataException($"{typeof(TRecord).Name} manifest have null resources.");

        Console.WriteLine($"Loading Assets - ({typeof(TRecord).Name})");

        foreach (var entry in manifest.Resources)
        {
            var asset = loader(entry);
            var key = AssetKey.For<TResult>(asset.Name);
            if(!_store.TryAdd(key, asset))
                throw new InvalidOperationException($"Asset '{asset.Name}' is already exists.");
            
            onAdd?.Invoke(asset);
        }
    }

    private void LoadMaterialStore(string manifestFilename, AssetLoader loader)
    {
        var result = new List<MaterialTemplate>();
        LoadEntries<AssetMaterialTemplate, MaterialTemplate>(
            manifestFilename,
            MaterialHandler,
            (mat) => result.Add(mat));

        _materialStore = new MaterialStore(result);
        return;

        MaterialTemplate MaterialHandler(AssetMaterialTemplate template) =>
            loader.LoadMaterialTemplate(template, Get<Shader>, Get<Texture2D>);
    }
}