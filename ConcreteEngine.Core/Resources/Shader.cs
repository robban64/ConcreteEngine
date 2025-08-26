#region

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Resources;

public sealed class Shader : IGraphicAssetFile<ShaderId>
{
    public required string Name { get; init; }
    public required string VertShaderFilename { get; init; }
    public required string FragShaderFilename { get; init; }
    public required ShaderId ResourceId { get; init; }
    public required int Samplers { get; init; }
    public required UniformTable UniformTable { get; init; }
    public IReadOnlyList<ShaderUniform> Uniforms => UniformTable.Uniforms;

    public AssetFileType AssetType => AssetFileType.Shader;
}