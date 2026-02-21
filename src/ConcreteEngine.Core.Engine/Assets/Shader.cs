using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class Shader : AssetObject
{
    public required ShaderId GfxId { get; init; }
    public int Samplers { get; init; }

    public override AssetKind Kind => AssetKind.Shader;
    public override AssetCategory Category => AssetCategory.Graphic;

    public override AssetObject CopyAndIncreaseGen()
    {
        return new Shader(){Id = Id, GId = GId, Name = Name, GfxId = GfxId, Samplers = Samplers, Generation = Generation+1};
    }
}