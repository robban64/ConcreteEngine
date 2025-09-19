#region

using ConcreteEngine.Core.Assets.Loaders;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Assets;

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

    private delegate TResult AssetUploadHandler<in TRecord, TPayload, out TResult>(
        IGfxResourceAllocator allocator,
        IGfxFactoryHub factoryHub,
        TRecord record,
        in TPayload payload)
        where TRecord : class, IAssetManifestRecord
        where TPayload : struct, allows ref struct
        where TResult : class, IGraphicAssetFile;


    private const int ProcessOrderCount = 6;
    private readonly string _rootPath;

    private ShaderLoader _shaderLoader;
    private MeshLoader _meshLoader;
    private TextureLoader _textureLoader;
    private CubeMapLoader _cubeMapLoader;

    private IGfxResourceAllocator _gpuAllocator;
    private IGfxFactoryHub _gpuFactory;
    private AssetSystem _assetSystem;

    private ProcessOrder _processOrder = ProcessOrder.NotStarted;


    public bool HasStarted => _processOrder != ProcessOrder.NotStarted;
    public bool IsFinished => _processOrder == ProcessOrder.Finished;

    public AssetLoader(string rootPath, AssetSystem assetSystem)
    {
        _rootPath = rootPath;
        _assetSystem = assetSystem;
        //StbImage.stbi_set_flip_vertically_on_load(1);
    }

    internal void Start(AssetRecordResult assets, IGfxResourceAllocator allocator, IGfxFactoryHub gpuFactory)
    {
        _processOrder = (ProcessOrder)1;
        _gpuAllocator = allocator;
        _gpuFactory = gpuFactory;
        _shaderLoader = new ShaderLoader(assets.Shaders.Resources);
        _meshLoader = new MeshLoader(assets.Meshes.Resources);
        _textureLoader = new TextureLoader(assets.Textures.Resources);
        _cubeMapLoader = new CubeMapLoader(assets.Cubemaps.Resources);

        AssetUploadRegistry<MeshManifestRecord, MeshLoaderResult, Mesh>
            .Register(GpuUploaders.UploadMesh);
        AssetUploadRegistry<TextureManifestRecord, TexturePayload, Texture2D>
            .Register(GpuUploaders.UploadTexture);
        AssetUploadRegistry<CubeMapManifestRecord, CubeMapPayload, CubeMap>
            .Register(GpuUploaders.UploadCubeMap);
        AssetUploadRegistry<ShaderManifestRecord, GpuShaderData, Shader>
            .Register(GpuUploaders.UploadShader);
    }

    internal void Finish()
    {
        _shaderLoader.Finish();
        _meshLoader.Finish();
        _textureLoader.Finish();
        _cubeMapLoader.Finish();

        _gpuAllocator = null!;
        _gpuFactory = null!;
        _assetSystem = null!;

        AssetUploadRegistry<MeshManifestRecord, MeshLoaderResult, Mesh>.Unregister();
        AssetUploadRegistry<TextureManifestRecord, TexturePayload, Texture2D>.Unregister();
        AssetUploadRegistry<CubeMapManifestRecord, CubeMapPayload, CubeMap>.Unregister();
        AssetUploadRegistry<ShaderManifestRecord, GpuShaderData, Shader>.Unregister();
    }

    public bool Process()
    {
        switch (_processOrder)
        {
            case ProcessOrder.NotStarted:
                throw new InvalidOperationException("Asset loader has not started.");
            case ProcessOrder.Shaders:
                 ProcessLoader<ShaderManifestRecord, GpuShaderData, Shader>(_shaderLoader);
                break;
            case ProcessOrder.Textures:
                 ProcessLoader<TextureManifestRecord, TexturePayload, Texture2D>(_textureLoader);
                break;
            case ProcessOrder.CubeMaps:
                 ProcessLoader<CubeMapManifestRecord, CubeMapPayload, CubeMap>(_cubeMapLoader);
                break;
            case ProcessOrder.Meshes:
                 ProcessLoader<MeshManifestRecord, MeshLoaderResult, Mesh>(_meshLoader);
                break;
            case ProcessOrder.Finished:
                return true;
        }

        return false;
    }

    public void ProcessLoader<TRecord, TPayload, TResult>(AssetTypeLoader<TRecord, TPayload> loader)
        where TRecord : class, IAssetManifestRecord
        where TPayload : struct, allows ref struct
        where TResult : class, IGraphicAssetFile
    {
        if (loader == null)
        {
            _processOrder = (ProcessOrder)((int)_processOrder + 1);
            return;
        }
        
        if (!loader.ProcessNext(out var record, out var payload))
        {
            var order = (int)_processOrder + 1;
            if (order >= ProcessOrderCount)
                _processOrder = ProcessOrder.Finished;
            else
                _processOrder = (ProcessOrder)order;

            return;
        }


        var handler = AssetUploadRegistry<TRecord, TPayload, TResult>.Handler;
        if (handler == null)
        {
            var inner = $"{typeof(TRecord).Name}, {typeof(TPayload).Name}, {typeof(TResult).Name}";
            throw new NotSupportedException($"No upload handler registered for ({inner}).");
        }

        var asset = handler(_gpuAllocator, _gpuFactory, record!, in payload);
        if (asset is Texture2D texture)
        {
            if (_textureLoader.DataCache.TryGetValue(asset.Name, out var data))
            {
                texture.SetPixelData(data);
            }
        }
        _assetSystem.AddResource(asset);
    }


    private static class AssetUploadRegistry<TRecord, TPayload, TResult>
        where TRecord : class, IAssetManifestRecord
        where TPayload : struct, allows ref struct
        where TResult : class, IGraphicAssetFile
    {
        public static AssetUploadHandler<TRecord, TPayload, TResult>? Handler;

        public static void Register(AssetUploadHandler<TRecord, TPayload, TResult> handler) => Handler = handler;

        public static void Unregister() => Handler = null;
    }

    private static class GpuUploaders
    {
        public static Mesh UploadMesh(IGfxResourceAllocator allocator, IGfxFactoryHub factoryHub, MeshManifestRecord record, in MeshLoaderResult payload)
        {
            
            var vbo = new GpuVboDescriptor<Vertex3D>(payload.MeshData.Vertices, BufferUsage.StaticDraw);
            var ibo = new GpuIboDescriptor<uint>(payload.MeshData.Indices,  BufferUsage.StaticDraw);
            var desc = GpuMeshDescriptor.MakeElemental(payload.Descriptor.Attributes, payload.Descriptor.ElementSize, payload.Descriptor.Primitive,
                payload.Descriptor.DrawCount);
            
            var builder = factoryHub.MeshFactory;
            var result = builder.CreateElementalMesh(vbo, ibo,desc);
            return new Mesh
            {
                Name = record.Name,
                Filename = record.Filename,
                DrawCount = desc.DrawCount,
                ResourceId = result.MeshId
            };
        }

        public static Texture2D UploadTexture(IGfxResourceAllocator allocator, IGfxFactoryHub factoryHub, TextureManifestRecord record,
            in TexturePayload payload)
        {
            var id = allocator.CreateTexture2D(payload.Data, in payload.Descriptor, out var meta);
            
            //var data = record.InMemory ? _dataCache[record.Name] : null;
            return new Texture2D
            {
                Name = record.Name,
                Path = record.Filename,
                ResourceId = id,
                Width = meta.Width,
                Height = meta.Height,
                PixelFormat = meta.Format,
                Preset = record.Preset,
                Anisotropy = record.Anisotropy
            };
        }

        public static CubeMap UploadCubeMap(IGfxResourceAllocator allocator, IGfxFactoryHub factoryHub, CubeMapManifestRecord record,
            in CubeMapPayload payload)
        {
            var id = allocator.CreateCubeMap(payload.Data, in payload.Descriptor, out var meta);
            return new CubeMap
            {
                Name = record.Name,
                ResourceId = id,
                Width = meta.Width,
                Height = meta.Height,
                PixelFormat = meta.Format,
                Textures = record.Textures
            };
        }

        public static Shader UploadShader(IGfxResourceAllocator allocator, IGfxFactoryHub factoryHub, ShaderManifestRecord record,
            in GpuShaderData data)
        {
            var id = allocator.CreateShader(data.VertexSource, data.FragmentSource, out var meta);
            return new Shader
            {
                Name = record.Name,
                FragShaderFilename = record.FragmentFilename,
                VertShaderFilename = record.VertexFilename,
                ResourceId = id,
                Samplers = meta.Samplers
            };
        }
    }


    private string GetFilePath(string assetTypePath, string fileName)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), _rootPath, assetTypePath, fileName);
        if (!File.Exists(path)) throw new FileNotFoundException($"Asset Resource Path not found", path);
        return path;
    }
}