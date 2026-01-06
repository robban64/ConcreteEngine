using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets;

public sealed record Shader : AssetObject
{
    public AssetRef<Shader> RefId => new(Id);
    public required ShaderId ShaderId { get; init; }
    public int Samplers { get; init; }

    internal Shader()
    {
    }

    public override AssetKind Kind => AssetKind.Shader;
    public override AssetCategory Category => AssetCategory.Graphic;
    public GraphicsKind GraphicsKind => GraphicsKind.Shader;

}