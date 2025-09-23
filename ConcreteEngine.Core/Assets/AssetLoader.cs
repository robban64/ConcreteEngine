#region

using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Assets.Loaders;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Assets;

internal delegate TResult AssetUploadHandler<in TRecord, TPayload, out TResult>(TRecord record, in TPayload payload)
    where TRecord : class, IAssetManifestRecord
    where TResult : class, IGraphicAssetFile;

internal sealed class AssetLoader
{
    private enum ProcessOrder
    {
        NotStarted,
        Shaders,
        Textures,
        CubeMaps,
        Meshes,
        Finished
    }

    private const int ProcessOrderCount = 6;
    private readonly string _rootPath;

    private AssetGfxUploader _uploader;

    private ShaderLoader _shaderLoader;
    private MeshLoader _meshLoader;
    private TextureLoader _textureLoader;
    private CubeMapLoader _cubeMapLoader;

    private ProcessOrder _processOrder = ProcessOrder.NotStarted;
   // private FrozenTypeRegistry<IAssetManifestRecord, IAssetTypeLoader> _loaders;

    public AssetLoader(string rootPath, AssetGfxUploader uploader)
    {
        _rootPath = rootPath;
        _uploader = uploader;
    }

    internal void Start(AssetRecordResult assets)
    {
        _processOrder = (ProcessOrder)1;
        _shaderLoader = new ShaderLoader(assets.Shaders.Resources);
        _meshLoader = new MeshLoader(assets.Meshes.Resources);
        _textureLoader = new TextureLoader(assets.Textures.Resources);
        _cubeMapLoader = new CubeMapLoader(assets.Cubemaps.Resources);

    /*
        _loaders = new FrozenTypeRegistry<IAssetManifestRecord, IAssetTypeLoader>();
        _loaders.Register<ShaderManifestRecord>(_shaderLoader);
        _loaders.Register<TextureManifestRecord>(_textureLoader);
        _loaders.Register<CubeMapManifestRecord>(_cubeMapLoader);
        _loaders.Register<MeshManifestRecord>(_meshLoader);
        _loaders.Freeze();


        AssetUploadRegistry<MeshManifestRecord, MeshLoaderResult, Mesh>
            .Register(_uploader.UploadMesh);
        AssetUploadRegistry<TextureManifestRecord, TexturePayload, Texture2D>
            .Register(_uploader.UploadTexture);
        AssetUploadRegistry<CubeMapManifestRecord, CubeMapPayload, CubeMap>
            .Register(_uploader.UploadCubeMap);
        AssetUploadRegistry<ShaderManifestRecord, TempShaderPayload, Shader>
            .Register(_uploader.UploadShader);
*/
    }

    internal void Finish()
    {
        _shaderLoader.Finish();
        _meshLoader.Finish();
        _textureLoader.Finish();
        _cubeMapLoader.Finish();

        _uploader = null!;
/*
        AssetUploadRegistry<MeshManifestRecord, MeshLoaderResult, Mesh>.Unregister();
        AssetUploadRegistry<TextureManifestRecord, TexturePayload, Texture2D>.Unregister();
        AssetUploadRegistry<CubeMapManifestRecord, CubeMapPayload, CubeMap>.Unregister();
        AssetUploadRegistry<ShaderManifestRecord, TempShaderPayload, Shader>.Unregister();
*/
    }

    public bool Process(out IAssetLoadEntry loadEntry)
    {
        loadEntry = null!;

        switch (_processOrder)
        {
            case ProcessOrder.NotStarted:
                throw new InvalidOperationException("Asset loader has not started.");
            case ProcessOrder.Shaders:
                var shaderEntry = ProcessLoader<ShaderManifestRecord, ShaderPayload, Shader>(_shaderLoader);
                _uploader.UploadShader(shaderEntry.Record, shaderEntry.Payload);
                break;
            case ProcessOrder.Textures:
                var texEntry = ProcessLoader<TextureManifestRecord, TexturePayload, Texture2D>(_textureLoader);
                _uploader.UploadTexture(texEntry.Record, texEntry.Payload);
                break;
            case ProcessOrder.CubeMaps:
                var cubeMapEntry = ProcessLoader<CubeMapManifestRecord, CubeMapPayload, CubeMap>(_cubeMapLoader);
                _uploader.UploadCubeMap(cubeMapEntry.Record, cubeMapEntry.Payload);
                break;
            case ProcessOrder.Meshes:
                var meshEntry = ProcessLoader<MeshManifestRecord, MeshLoaderResult, Mesh>(_meshLoader);
                _uploader.UploadMesh(meshEntry.Record, meshEntry.Payload);
                break;
            case ProcessOrder.Finished:
                return true;
        }

        return false;
    }

    private AssetLoadEntry<TRecord, TPayload> ProcessLoader<TRecord, TPayload, TResult>(
        AssetTypeLoader<TRecord, TPayload> loader)
        where TRecord : class, IAssetManifestRecord where TResult : class, IGraphicAssetFile
    {
        if (!loader.ProcessNext(out var loadEntry))
        {
            var order = (int)_processOrder + 1;
            if (order >= ProcessOrderCount)
                _processOrder = ProcessOrder.Finished;
            else
                _processOrder = (ProcessOrder)order;

            return null!;
        }

        if (loadEntry.Payload == null) throw new NullReferenceException(nameof(loadEntry.Payload));
        return new AssetLoadEntry<TRecord, TPayload>(){Record = r}
        var handler = AssetUploadRegistry<TRecord, TPayload, TResult>.Handler;
        if (handler == null)
        {
            var inner = $"{typeof(TRecord).Name}, {typeof(TPayload).Name}, {typeof(TResult).Name}";
            throw new NotSupportedException($"No upload handler registered for ({inner}).");
        }

        var asset = handler(loadEntry.Record, loadEntry.Payload);


        if (asset is Texture2D texture)
        {
            if (_textureLoader.DataCache.TryGetValue(asset.Name, out var data))
            {
                texture.SetPixelData(data);
            }
        }

        return loadEntry;
    }


    private string GetFilePath(string assetTypePath, string fileName)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), _rootPath, assetTypePath, fileName);
        if (!File.Exists(path)) throw new FileNotFoundException($"Asset Resource Path not found", path);
        return path;
    }
}