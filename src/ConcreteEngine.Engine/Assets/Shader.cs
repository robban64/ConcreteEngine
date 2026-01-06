using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Metadata.Asset;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets;

public sealed class Shader : AssetObject
{

    public AssetRef<Shader> RefId => new(Id);
    public new required ShaderId ResourceId { get; init; }
    public int Samplers { get; internal set; }

    internal Shader()
    {
    }

    public override AssetKind Kind => AssetKind.Shader;
    public override AssetCategory Category => AssetCategory.Graphic;
    public GraphicsKind GraphicsKind => GraphicsKind.Shader;

    internal void OnReload(int samplers)
    {
        Samplers = samplers;
    }
}