#region

using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using StbImageSharp;

#endregion

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetLoader
{
    private readonly string _rootPath;
    private readonly IGraphicsDevice _graphics;

    public AssetLoader(IGraphicsDevice graphics, string rootPath)
    {
        _graphics = graphics;
        _rootPath = rootPath;
        //StbImage.stbi_set_flip_vertically_on_load(1);
    }

    private string GetPath(string assetTypePath, string fileName) =>
        Path.Combine(_rootPath, assetTypePath, fileName);
    

    public Texture2D LoadTexture2D(AssetTextureRecord record)
    {
        using var stream = File.OpenRead(GetPath("textures", record.Path));
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
            Path = record.Path,
            ResourceId = resourceId,
            Width = textureData.Width,
            Height = textureData.Height,
            PixelFormat = textureData.Format,
            Preset = record.Preset
        };
    }
    
    public Shader LoadShader(AssetShaderRecord record)
    {
        var vertexSource = File.ReadAllText(GetPath("shaders", record.VertShaderPath));
        var fragmentSource = File.ReadAllText(GetPath("shaders", record.FragShaderPath));

        var resourceId = _graphics.CreateShader(vertexSource, fragmentSource, record.Samplers ?? []);
        var uniforms = _graphics.GetShaderUniforms(resourceId);

        return new Shader
        {
            Name = record.Name,
            VertShaderFilename = record.VertShaderPath,
            FragShaderFilename = record.FragShaderPath,
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
            Color = record.Color,
        };
    }
}