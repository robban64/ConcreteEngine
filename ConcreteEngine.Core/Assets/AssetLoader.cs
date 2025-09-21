#region

using System.Runtime.InteropServices;
using ConcreteEngine.Core.Assets.Loaders;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Assets;

// TODO rework this mess
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
        GfxContext gfx,
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

    private GfxContext _gfx;
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

    internal void Start(AssetRecordResult assets, GfxContext gfx)
    {
        _processOrder = (ProcessOrder)1;
        _gfx = gfx;
        _shaderLoader = new ShaderLoader(assets.Shaders.Resources);
        _meshLoader = new MeshLoader(assets.Meshes.Resources);
        _textureLoader = new TextureLoader(assets.Textures.Resources);
        _cubeMapLoader = new CubeMapLoader(assets.Cubemaps.Resources);

        AssetUploadRegistry<MeshManifestRecord, MeshLoaderResult, Mesh>
            .Register(GpuUploaders.UploadMesh);
        AssetUploadRegistry<TextureManifestRecord, TexturePayload, Texture2D>
            .Register(GpuUploaders.UploadTexture);
        AssetUploadRegistry<ShaderManifestRecord, TempShaderPayload, Shader>
            .Register(GpuUploaders.UploadShader);
    }

    internal void Finish()
    {
        _shaderLoader.Finish();
        _meshLoader.Finish();
        _textureLoader.Finish();
        _cubeMapLoader.Finish();

        _gfx = null!;
        _assetSystem = null!;

        AssetUploadRegistry<MeshManifestRecord, MeshLoaderResult, Mesh>.Unregister();
        AssetUploadRegistry<TextureManifestRecord, TexturePayload, Texture2D>.Unregister();
        AssetUploadRegistry<ShaderManifestRecord, TempShaderPayload, Shader>.Unregister();
    }

    public bool Process()
    {
        switch (_processOrder)
        {
            case ProcessOrder.NotStarted:
                throw new InvalidOperationException("Asset loader has not started.");
            case ProcessOrder.Shaders:
                 ProcessLoader<ShaderManifestRecord, TempShaderPayload, Shader>(_shaderLoader);
                break;
            case ProcessOrder.Textures:
                 ProcessLoader<TextureManifestRecord, TexturePayload, Texture2D>(_textureLoader);
                break;
            case ProcessOrder.CubeMaps:
                ProcessCubeMapLoader(_cubeMapLoader);
                 //ProcessLoader<CubeMapManifestRecord, TexturePayload, CubeMap>(_cubeMapLoader);
                break;
            case ProcessOrder.Meshes:
                 ProcessLoader<MeshManifestRecord, MeshLoaderResult, Mesh>(_meshLoader);
                break;
            case ProcessOrder.Finished:
                return true;
        }

        return false;
    }

    // Temp solution
    private void ProcessCubeMapLoader(CubeMapLoader loader)
    {
        if (loader == null)
        {
            _processOrder = (ProcessOrder)((int)_processOrder + 1);
            return;
        }

        TexturePayload payload = default;
        if (!loader.ProcessNext(out var record, out payload))
        {
            var order = (int)_processOrder + 1;
            if (order >= ProcessOrderCount)
                _processOrder = ProcessOrder.Finished;
            else
                _processOrder = (ProcessOrder)order;

            return;
        }

        var desc = payload.Descriptor;
        var textureId = _gfx.Textures.CreateCubeMap(in desc);
        _gfx.Textures.UploadCubeMapFace(textureId, payload.Data, desc.Width,desc.Height,0);
        for (int i = 1; i < 6; i++)
        {
            var face = loader.LoadFaceData(record!, i);
            _gfx.Textures.UploadCubeMapFace(textureId, face.Data, face.Descriptor.Width,face.Descriptor.Height,i);
        }

        var cubeMap = new CubeMap
        {
            Name = record!.Name,
            ResourceId = textureId,
            Textures = record.Textures,
            Width = record.Width,
            Height = record.Height,
            PixelFormat = payload.Descriptor.Format
        };
        
        _assetSystem.AddResource(cubeMap);

    }

    private void ProcessLoader<TRecord, TPayload, TResult>(AssetTypeLoader<TRecord, TPayload> loader)
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

        var asset = handler(_gfx, record!, in payload);

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
        public static Mesh UploadMesh(GfxContext gfx, MeshManifestRecord record, in MeshLoaderResult payload)
        {
            /*
            var vbo = new GpuVboDescriptor<Vertex3D>(payload.MeshData.Vertices, BufferUsage.StaticDraw);
            var ibo = new GpuIboDescriptor<uint>(payload.MeshData.Indices,  BufferUsage.StaticDraw);
            var desc = GpuMeshDescriptor.MakeElemental(payload.Properties.Attributes, payload.Properties.ElementSize, payload.Properties.Primitive,
                payload.Properties.DrawCount);
            
            var builder = factoryHub.MeshFactory;
            var result = builder.CreateElementalMesh(vbo, ibo,desc);
*/
            ReadOnlySpan<Vertex3D> vSpan = CollectionsMarshal.AsSpan(payload.Vertices);
            ReadOnlySpan<uint> iSpan = CollectionsMarshal.AsSpan(payload.Indices);

            var builder = gfx.Meshes.StartUploadBuilder(payload.Properties);
            builder.UploadVertices(vSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
            builder.UploadIndices(iSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
            builder.SetAttributeRange(payload.Attributes);
            var meshId = builder.Finish();
            
            return new Mesh
            {
                Name = record.Name,
                Filename = record.Filename,
                DrawCount = payload.Properties.DrawCount,
                ResourceId = meshId
            };
        }

        public static Texture2D UploadTexture(GfxContext gfx, TextureManifestRecord record,in TexturePayload payload)
        {
            var id = gfx.Textures.CreateTexture(payload.Data, in payload.Descriptor);
            
            //var data = record.InMemory ? _dataCache[record.Name] : null;
            return new Texture2D
            {
                Name = record.Name,
                Path = record.Filename,
                ResourceId = id,
                Width = payload.Descriptor.Width,
                Height = payload.Descriptor.Height,
                PixelFormat = payload.Descriptor.Format,
                Preset = record.Preset,
                Anisotropy = record.Anisotropy
            };
        }

        public static Shader UploadShader(GfxContext gfx, ShaderManifestRecord record,in TempShaderPayload data)
        {
            var id = gfx.Shaders.CreateShader(data.Vs, data.Fs , out var samplers);
            
            return new Shader
            {
                Name = record.Name,
                FragShaderFilename = record.FragmentFilename,
                VertShaderFilename = record.VertexFilename,
                ResourceId = id,
                Samplers = samplers
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