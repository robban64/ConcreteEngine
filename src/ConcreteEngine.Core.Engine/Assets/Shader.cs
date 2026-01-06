using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed record Shader : AssetObject
{
    public required ShaderId ShaderId { get; init; }
    public int Samplers { get; init; }

    public override AssetKind Kind => AssetKind.Shader;
    public override AssetCategory Category => AssetCategory.Graphic;
    public GraphicsKind GraphicsKind => GraphicsKind.Shader;

}