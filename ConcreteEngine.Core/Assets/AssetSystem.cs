#region

using System.Text.Json;
using System.Text.Json.Serialization;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Assets;

public sealed class AssetSystem(
    IGraphicsDevice graphics,
    string assetPath = "assets",
    string manifestFilename = "manifest.json"
) : IGameEngineSystem
{
    private readonly Dictionary<string, IAssetFile> _store = new();

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
            if(asset is T typedAsset) result.Add(typedAsset);
        }
        return result;
    }
    
    internal void LoadFromManifest()
    {
        if (!Directory.Exists(assetPath))
        {
            throw new DirectoryNotFoundException($"Asset manifest '{assetPath}' directory not found.");
        }

        var manifestPath = Path.Combine(assetPath, manifestFilename);
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Manifest '{manifestPath}' not found.");
        }

        Console.WriteLine("Loading asset manifest...");
        var json = File.ReadAllText(manifestPath);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
        
        var assetEntries = JsonSerializer.Deserialize<AssetManifest>(json, jsonOptions) ??
                           throw new InvalidDataException("Invalid manifest.");

        var loader = new AssetLoader(graphics, assetPath);

        LoadEntries(assetEntries.Shaders, loader.LoadShader);
        LoadEntries(assetEntries.Textures, loader.LoadTexture2D);
        Console.WriteLine("Asset manifest loaded. " + _store.Count);
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

    public void Dispose()
    {
        foreach (var asset in _store.Values)
        {
            if (asset is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private void LoadEntries<T, R>(List<T> entries, Func<T, R> loader)
        where T : AssetManifestRecord where R : class, IAssetFile
    {
        foreach (var entry in entries)
        {
            Console.WriteLine($"Loading entry({entry.Name}) at {entry.Path}");
            var result = loader(entry);
            _store.TryAdd(result.Name, result);
        }
    }
}