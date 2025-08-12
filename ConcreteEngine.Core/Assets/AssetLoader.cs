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

    private string GetEntryPath(string assetTypePath, AssetManifestEntry entry) =>
        Path.Combine(_rootPath, assetTypePath, entry.Path);

    public Shader LoadShader(AssetShaderEntry entry)
    {
        var shaderText = File.ReadAllText(GetEntryPath("shaders", entry));

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

        var resourceId = _graphics.CreateShader(vertexSource, fragmentSource);

        return new Shader
        {
            Name = entry.Name,
            Path = entry.Path,
            ResourceId = resourceId
        };
    }

    public Texture2D LoadTexture2D(AssetTextureEntry entry)
    {
        using var stream = File.OpenRead(GetEntryPath("textures", entry));
        var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        var textureData = new TextureDescriptor
        (
            pixelData: result.Data,
            width: result.Width,
            height: result.Height,
            format: entry.PixelFormat
        );

        var resourceId = _graphics.CreateTexture2D(in textureData);

        return new Texture2D
        {
            Name = entry.Name,
            Path = entry.Path,
            Width = textureData.Width,
            Height = textureData.Height,
            PixelFormat = textureData.Format,
            ResourceId = resourceId
        };
    }
}