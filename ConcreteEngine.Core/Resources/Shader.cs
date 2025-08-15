#region

using ConcreteEngine.Core.Assets;

#endregion

namespace ConcreteEngine.Core.Resources;

public sealed class Shader : IGraphicAssetFile
{
    public required string Name { get; init; }
    public required string VertShaderFilename { get; init; }
    public required string FragShaderFilename { get; init; }

    public required ushort ResourceId { get; init; }
    
    public required int Samplers { get; init; }
    public AssetFileType AssetType => AssetFileType.Shader;

}