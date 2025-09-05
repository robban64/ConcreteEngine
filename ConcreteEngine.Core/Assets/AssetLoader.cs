#region

using System.Text;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;
using StbImageSharp;

#endregion

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetLoader
{
    private readonly string _rootPath;
    private readonly IGraphicsDevice _graphics;
    private readonly MeshLoader _meshLoader;

    private readonly Dictionary<string, string> _vertexShaderCache = new(StringComparer.Ordinal);

    public AssetLoader(IGraphicsDevice graphics, string rootPath)
    {
        _graphics = graphics;
        _rootPath = rootPath;
        _meshLoader = new MeshLoader();
        //StbImage.stbi_set_flip_vertically_on_load(1);
    }

    public void ClearCache()
    {
        _vertexShaderCache.Clear();
        _meshLoader.ClearCache();
    }

    private string GetFilePath(string assetTypePath, string fileName)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), _rootPath, assetTypePath, fileName);
        if (!File.Exists(path)) throw new FileNotFoundException($"Asset Resource Path not found", path);
        return path;
    }
    
    public Mesh LoadMesh(AssetMeshRecord record)
    {
        var data = _meshLoader.LoadModel(GetFilePath("meshes", record.Filename));
        var descriptor = new MeshDescriptor<Vertex3D, uint>
        {
            VertexBuffer = new MeshDataBufferDescriptor<Vertex3D>(BufferUsage.StaticDraw, data.Vertices),
            IndexBuffer = new MeshDataBufferDescriptor<uint>(BufferUsage.StaticDraw, data.Indices),
            VertexPointers =
            [
                VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.Position), VertexElementFormat.Float3),
                VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.TexCoords), VertexElementFormat.Float2),
                VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.Normal), VertexElementFormat.Float3),
                VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.Tangent), VertexElementFormat.Float3),

            ]
        };

        var meshId = _graphics.CreateMesh(descriptor, out var meta);
        return new Mesh
        {
            Name = record.Name,
            Filename = record.Filename,
            ResourceId = meshId,
            Meta = meta,
        };

    }


    public Texture2D LoadTexture2D(AssetTextureRecord record)
    {
        using var stream = File.OpenRead(GetFilePath("textures", record.Filename));
        var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        ValidateImageResult(result);

        var textureData = new TextureDesc
        (
            PixelData: result.Data,
            Width: result.Width,
            Height: result.Height,
            Format: record.PixelFormat,
            Preset: record.Preset,
            LodBias: record.LodBias,
            Anisotropy: record.Anisotropy
        );

        var resourceId = _graphics.CreateTexture2D(in textureData);

        return new Texture2D
        {
            Name = record.Name,
            Path = record.Filename,
            ResourceId = resourceId,
            Width = textureData.Width,
            Height = textureData.Height,
            PixelFormat = textureData.Format,
            Preset = record.Preset,
            Anisotropy = record.Anisotropy,
            Data = record.InMemory ? result.Data : null
        };
    }

    public CubeMap LoadCubeMap(AssetCubeMapRecord record)
    {
        ArgumentNullException.ThrowIfNull(record, nameof(record));
        ArgumentNullException.ThrowIfNull(record.Textures, nameof(record.Textures));
        ArgumentOutOfRangeException.ThrowIfNotEqual(record.Textures.Length, 6, nameof(record.Textures));
        ArgumentOutOfRangeException.ThrowIfNotEqual(record.Width, record.Height);

        var loaders = new Func<CubemapFaceDesc>[6];
        for (var i = 0; i < record.Textures.Length; i++)
        {
            var filename = record.Textures[i];
            loaders[i] = () => CubeMapFaceLoader(filename);
        }

        var desc = new CreateCubemapDesc(loaders, record.Width, record.Height, record.PixelFormat);
        var resourceId = _graphics.CreateCubeMap(in desc);
        
        return new CubeMap
        {
            Name = record.Name,
            ResourceId = resourceId,
            Width = record.Width,
            Height = record.Height,
            PixelFormat = EnginePixelFormat.Rgba,
            Textures = record.Textures,
        };

        CubemapFaceDesc CubeMapFaceLoader(string filename)
        {
            using var stream = File.OpenRead(GetFilePath("cubemaps", filename));
            var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            ValidateImageResult(result);
            return new CubemapFaceDesc(
                PixelData: result.Data,
                Width: result.Width,
                Height: result.Height,
                Format: EnginePixelFormat.Rgba
            );

        }
    }

    private static void ValidateImageResult(ImageResult result)
    {
        ArgumentNullException.ThrowIfNull(result, nameof(result));
        ArgumentNullException.ThrowIfNull(result.Data, nameof(result.Data));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Data.Length, 0, nameof(result.Data));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Width, 0, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Height, 0, nameof(result.Height));
    }


    public Shader LoadShader(AssetShaderRecord record)
    {
        if (!_vertexShaderCache.TryGetValue(record.VertexFilename, out var vertexSource))
        {
            var path = GetFilePath("shaders", record.VertexFilename);
            _vertexShaderCache[record.VertexFilename] = vertexSource = File.ReadAllText(path);
        }

        var fragmentSource = File.ReadAllText(GetFilePath("shaders", record.FragmentFilename));

        var resourceId = _graphics.CreateShader(vertexSource, fragmentSource, record.Samplers ?? []);
        var uniforms = _graphics.GetShaderUniforms(resourceId);

        return new Shader
        {
            Name = record.Name,
            VertShaderFilename = record.VertexFilename,
            FragShaderFilename = record.FragmentFilename,
            ResourceId = resourceId,
            Samplers = record.Samplers?.Length ?? 0,
            UniformTable = uniforms
        };
    }

    public MaterialTemplate LoadMaterialTemplate(AssetMaterialTemplate record, Func<string, Shader> getShader,
        Func<string, Texture2D> getTexture)
    {
        Texture2D[] textures = [];
        if (record.Textures != null)
        {
            textures = new Texture2D[record.Textures.Length];
            for (var i = 0; i < record.Textures.Length; i++)
            {
                textures[i] = getTexture(record.Textures[i]);
            }
        }

        var valuesCount = record.Defaults?.Count ?? 4;
        Dictionary<ShaderUniform, IMaterialValue> materialValues = new(valuesCount);

        if (record.Defaults != null)
        {
            foreach (var (uniform, value) in record.Defaults)
            {
                materialValues.Add(uniform, value.ToMaterialValue());
            }
        }


        var template = new MaterialTemplate(materialValues)
        {
            Name = record.Name,
            Shader = getShader(record.Shader),
            Textures = textures,
            Color = record.Color
        };
        template.Process();
        return template;
    }

}