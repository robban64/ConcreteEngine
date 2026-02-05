using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets;

public sealed class Shader : AssetObject, IShader
{
    public required ShaderId GfxId { get; init; }
    public int Samplers { get; init; }

    public override AssetKind Kind => AssetKind.Shader;
    public override AssetCategory Category => AssetCategory.Graphic;

    internal override AssetObject CopyAndIncreaseGen()
    {
        return new Shader(){Id = Id, GId = GId, Name = Name, GfxId = GfxId, Samplers = Samplers, Generation = Generation+1};
    }
}