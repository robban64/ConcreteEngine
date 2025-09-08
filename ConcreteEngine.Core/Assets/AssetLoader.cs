#region

using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConcreteEngine.Core.Assets.IO;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using StbImageSharp;

#endregion

namespace ConcreteEngine.Core.Assets;

public interface IAssetTypeLoader
{
    public bool HasStarted { get; }
    public bool IsFinished { get; }
}

internal sealed class AssetLoader
{
    private readonly string _rootPath;

    private ShaderLoader _shaderLoader;
    private MeshLoader _meshLoader;
    private TextureLoader _textureLoader;
    private CubeMapLoader _cubeMapLoader;

    public bool HasStarted { get; private set; }

    public bool IsFinished =>
        _shaderLoader.IsFinished && _meshLoader.IsFinished && _textureLoader.IsFinished && _cubeMapLoader.IsFinished;

    public AssetLoader(string rootPath)
    {
        _rootPath = rootPath;
        //StbImage.stbi_set_flip_vertically_on_load(1);
    }

    public GpuResourcePayloadCollection CreateGpuPayloadCollection(AssetRecordResult assets)
    {
        HasStarted = true;

        _shaderLoader = new ShaderLoader(assets.Shaders);
        _meshLoader = new MeshLoader(assets.Meshes);
        _textureLoader = new TextureLoader(assets.Textures);
        _cubeMapLoader = new CubeMapLoader(assets.Cubemaps);

        return new GpuResourcePayloadCollection
        {
            Shaders = _shaderLoader, Meshes = _meshLoader, Textures = _textureLoader, CubeMaps = _cubeMapLoader
        };
    }

    internal AssetLoaderResult GetData()
    {
        if (!HasStarted) throw new InvalidOperationException("AssetLoader has not been started");
        if (!IsFinished) throw new InvalidOperationException("AssetLoader has not finished");

        return new AssetLoaderResult
        {
            Textures = _textureLoader.Results,
            CubeMaps = _cubeMapLoader.Results,
            Meshes = _meshLoader.Results,
            Shaders = _shaderLoader.Results,
        };
    }

    internal void ClearCache()
    {
        _shaderLoader.CleanCache();
        _meshLoader.ClearCache();
        _textureLoader.ClearCache();
        _cubeMapLoader.ClearCache();
    }

    private string GetFilePath(string assetTypePath, string fileName)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), _rootPath, assetTypePath, fileName);
        if (!File.Exists(path)) throw new FileNotFoundException($"Asset Resource Path not found", path);
        return path;
    }

    internal sealed record AssetLoaderResult
    {
        public required IReadOnlyList<Shader> Shaders { get; init; }
        public required IReadOnlyList<Mesh> Meshes { get; init; }
        public required IReadOnlyList<Texture2D> Textures { get; init; }
        public required IReadOnlyList<CubeMap> CubeMaps { get; init; }
    }
}