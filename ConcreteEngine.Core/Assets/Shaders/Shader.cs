#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Shaders;

public sealed class Shader : AssetObject
{
    public required ShaderId ResourceId { get; init; }
    public required int Samplers { get; init; }

    public override AssetKind Kind => AssetKind.Shader;
    public override AssetCategory Category  =>  AssetCategory.Graphic;
    public ResourceKind GfxResourceKind => ResourceKind.Shader;
}