#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Shaders;

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