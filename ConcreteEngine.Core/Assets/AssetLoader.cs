#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
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

    private string GetEntryPath(string assetTypePath, AssetManifestRecord record) =>
        Path.Combine(_rootPath, assetTypePath, record.Path);

    public Shader LoadShader(AssetShaderRecord record)
    {
        var shaderText = File.ReadAllText(GetEntryPath("shaders", record));

        var vertexIndex = shaderText.IndexOf("@vertex", StringComparison.Ordinal);
        var fragmentIndex = shaderText.IndexOf("@fragment", StringComparison.Ordinal);

        if (vertexIndex < 0)
            throw new InvalidDataException("Invalid vertex shader definition. Missing @vertex definition.");
        if (fragmentIndex < 0)
            throw new InvalidDataException("Invalid vertex shader definition. Missing @fragmentIndex definition.");


        var vertexSource = shaderText
            .Substring(vertexIndex + "@vertex".Length, fragmentIndex - vertexIndex - "@vertex".Length)
            .Trim();
        var fragmentSource = shaderText.Substring(fragmentIndex + "@fragment".Length).Trim();

        var resourceId = _graphics.CreateShader(vertexSource, fragmentSource, record.Samplers);

        return new Shader
        {
            Name = record.Name,
            Path = record.Path,
            ResourceId = resourceId,
            Samplers = record.Samplers.Length
        };
    }

    public Texture2D LoadTexture2D(AssetTextureRecord record)
    {
        using var stream = File.OpenRead(GetEntryPath("textures", record));
        var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        var textureData = new TextureDescriptor
        (
            PixelData: result.Data,
            Width: result.Width,
            Height: result.Height,
            Format: record.PixelFormat,
            Preset: record.Preset
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
}