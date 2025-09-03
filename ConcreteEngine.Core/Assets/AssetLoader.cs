#region

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

    private readonly Dictionary<string, string> _vertexShaderCache = new();

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
                VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.Position)),
                VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.TexCoords)),
                VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.Normal)),
                VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.Tangent)),

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

        var textureData = new TextureDesc
        (
            PixelData: result.Data,
            Width: result.Width,
            Height: result.Height,
            Format: record.PixelFormat,
            Preset: record.Preset,
            LodBias: record.LodBias
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
            Preset = record.Preset
        };
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


        return new MaterialTemplate(materialValues)
        {
            Name = record.Name,
            Shader = getShader(record.Shader),
            Textures = textures,
            Color = record.Color
        };
    }

}