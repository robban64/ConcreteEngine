using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Shaders;

public sealed class Shader : AssetObject
{
    public AssetRef<Shader> RefId => new(RawId);
    public new required ShaderId ResourceId { get; init; }
    public int Samplers { get; internal set; }

    internal Shader()
    {
    }

    public override AssetKind Kind => AssetKind.Shader;
    public override AssetCategory Category => AssetCategory.Graphic;
    public ResourceKind GfxResourceKind => ResourceKind.Shader;

    internal void OnReload(int samplers)
    {
        Samplers = samplers;
    }
}