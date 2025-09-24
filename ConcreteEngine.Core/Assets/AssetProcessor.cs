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

internal sealed class AssetProcessor
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

    public AssetProcessor(string rootPath, AssetGfxUploader uploader)
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
    }

    internal void Finish()
    {
        _shaderLoader.Finish();
        _meshLoader.Finish();
        _textureLoader.Finish();
        _cubeMapLoader.Finish();

        _uploader = null!;

    }

    public bool Process(out IAssetFinalEntry finalEntry)
    {
        finalEntry = null!;

        switch (_processOrder)
        {
            case ProcessOrder.NotStarted:
                throw new InvalidOperationException("Asset loader has not started.");
            case ProcessOrder.Shaders:
                var shaderEntry = ProcessLoader(_shaderLoader);
                if(shaderEntry == null) return false;
                var shaderId = _uploader.UploadShader(shaderEntry.Record, shaderEntry.Payload, out var shaderInfo);
                finalEntry = new AssetFinalEntry<ShaderManifestRecord, ShaderCreationInfo, ShaderId>
                    (shaderEntry.Record, in shaderInfo, shaderId, shaderEntry.ProcessInfo);
                break;
            case ProcessOrder.Textures:
                var texEntry = ProcessLoader(_textureLoader);
                if(texEntry == null) return false;
                var texId = _uploader.UploadTexture(texEntry.Record, texEntry.Payload, out var texInfo);
                finalEntry = new AssetFinalEntry<TextureManifestRecord, TextureCreationInfo, TextureId>
                    (texEntry.Record, in texInfo, texId, texEntry.ProcessInfo);

                break;
            case ProcessOrder.CubeMaps:
                var cubeMapEntry = ProcessLoader(_cubeMapLoader);
                if(cubeMapEntry == null) return false;
                var cubeMapId = _uploader.UploadCubeMap(cubeMapEntry.Record, cubeMapEntry.Payload, out var cubeMapInfo);
                finalEntry = new AssetFinalEntry<CubeMapManifestRecord, CubeMapCreationInfo, TextureId>
                    (cubeMapEntry.Record, in cubeMapInfo, cubeMapId, cubeMapEntry.ProcessInfo);

                break;
            case ProcessOrder.Meshes:
                var meshEntry = ProcessLoader(_meshLoader);
                if(meshEntry == null) return false;
                var meshId = _uploader.UploadMesh(meshEntry.Record, meshEntry.Payload, out var meshInfo);
                finalEntry = new AssetFinalEntry<MeshManifestRecord, MeshCreationInfo, MeshId>
                    (meshEntry.Record, in meshInfo, meshId, meshEntry.ProcessInfo);

                break;
            case ProcessOrder.Finished:
                return true;
        }

        return false;
    }

    private AssetLoadEntry<TRecord, TPayload> ProcessLoader<TRecord, TPayload>(
        AssetTypeLoader<TRecord, TPayload> loader)
        where TRecord : class, IAssetManifestRecord
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
        
        return loadEntry;
    }


    private string GetFilePath(string assetTypePath, string fileName)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), _rootPath, assetTypePath, fileName);
        if (!File.Exists(path)) throw new FileNotFoundException($"Asset Resource Path not found", path);
        return path;
    }
}