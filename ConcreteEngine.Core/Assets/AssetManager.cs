#region

using System.Text.Json;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Assets;

public sealed class AssetManager(
    IGraphicsDevice graphics,
    string assetPath = "assets",
    string manifestFilename = "manifest.json"
) : IDisposable
{
    private readonly Dictionary<string, IAssetFile> _store = new();

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
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var assetEntries = JsonSerializer.Deserialize<AssetManifest>(json, jsonOptions) ??
                           throw new InvalidDataException("Invalid manifest.");

        var loader = new AssetLoader(graphics, assetPath);

        LoadEntries(assetEntries.Shaders, loader.LoadShader);
        LoadEntries(assetEntries.Textures, loader.LoadTexture2D);
        Console.WriteLine("Asset manifest loaded. " + _store.Count);
    }

    public T Get<T>(string name) where T : class, IAssetFile
    {
        if (_store.TryGetValue(name, out var asset) && asset is T typed)
            return typed;

        throw new InvalidCastException($"Asset '{name}' not found or incorrect type.");
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
        where T : AssetManifestEntry where R : class, IAssetFile
    {
        foreach (var entry in entries)
        {
            Console.WriteLine($"Loading entry({entry.Name}) at {entry.Path}");
            var result = loader(entry);
            _store.TryAdd(result.Name, result);
        }
    }
}