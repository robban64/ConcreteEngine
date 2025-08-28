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
public sealed class AssetSystem :IAssetSystem
{
    private readonly Dictionary<string, IAssetFile> _store = new(64);
    private readonly IGraphicsDevice _graphics;
    private readonly string _assetPath;
    private readonly string _manifestFilename;

    private MaterialStore _materialStore = null!;
    private static bool _initialized = false;

    public MaterialStore MaterialStore => _materialStore;

    public AssetSystem(IGraphicsDevice graphics,
        string assetPath = "assets",
        string manifestFilename = "manifest.json")
    {
        _graphics = graphics;
        _assetPath = assetPath;
        _manifestFilename = manifestFilename;
    }

    public bool TryGet<T>(string name, out T resource) where T : class, IAssetFile
    {
        if (_store.TryGetValue(name, out var asset) && asset is T typed)
        {
            resource = typed;
            return true;
        }

        resource = null!;
        return false;
    }

    public T Get<T>(string name) where T : class, IAssetFile
    {
        if (_store.TryGetValue(name, out var asset) && asset is T typed)
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

        var manifestPath = Path.Combine(_assetPath, _manifestFilename);
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Manifest '{manifestPath}' not found.");
        }

        Console.WriteLine("Loading asset manifest...");
        var json = File.ReadAllText(manifestPath);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase), new Vector2Converter(), new Vector3Converter(),
                new Vector4Converter(),
                new MaterialValueConverter()
            }
        };

        var assetEntries = JsonSerializer.Deserialize<AssetManifest>(json, jsonOptions) ??
                           throw new InvalidDataException("Invalid manifest.");

        Console.WriteLine("Asset manifest loaded. " + _store.Count);

        var loader = new AssetLoader(_graphics, _assetPath);

        // Texture2D
        LoadEntries(assetEntries.Textures, loader.LoadTexture2D);

        // Shader
        LoadEntries(assetEntries.Shaders, loader.LoadShader);

        // Material
        LoadMaterialStore(assetEntries, loader);
    }

    private void LoadMaterialStore(AssetManifest assetEntries, AssetLoader loader)
    {
        var templates = LoadEntriesWithReturn(assetEntries.Materials, MaterialHandler);

        _materialStore = new MaterialStore(templates);
        return;

        MaterialTemplate MaterialHandler(AssetMaterialTemplate template) =>
            loader.LoadMaterialTemplate(template, Get<Shader>, Get<Texture2D>);
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

    private void LoadEntries<T, R>(List<T> entries, Func<T, R> loader)
        where T : IAssetManifestRecord where R : class, IAssetFile
    {
        Console.WriteLine($"Loading Store - ({typeof(T).Name})");

        foreach (var entry in entries)
        {
            var asset = loader(entry);
            _store.TryAdd(asset.Name, asset);
        }
    }

    private List<R> LoadEntriesWithReturn<T, R>(List<T> entries, Func<T, R> loader)
        where T : IAssetManifestRecord where R : class, IAssetFile
    {
        Console.WriteLine($"Loading Store - ({typeof(T).Name})");

        var result = new List<R>();
        foreach (var entry in entries)
        {
            var asset = loader(entry);
            _store.TryAdd(asset.Name, asset);
            result.Add(asset);
        }

        return result;
    }
}