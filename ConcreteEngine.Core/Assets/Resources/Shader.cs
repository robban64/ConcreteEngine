#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Resources;

public sealed class Shader : IGraphicAssetFile<ShaderId>
{
    public required string Name { get; init; }
    public required string VertShaderFilename { get; init; }
    public required string FragShaderFilename { get; init; }
    public required ShaderId ResourceId { get; init; }
    public required int Samplers { get; init; }

    public AssetKind Kind => AssetKind.Shader;
    public ResourceKind GfxResourceKind => ResourceKind.Shader;
}