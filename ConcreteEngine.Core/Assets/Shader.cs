#region

using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Assets;

public sealed class Shader : IGraphicAssetFile
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required IGraphicsResource GraphicsResource { get; init; }

    public AssetFileType AssetType => AssetFileType.Shader;

    public IShader ShaderProgram => (GraphicsResource as IShader)!;
}