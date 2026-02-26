using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class Shader(string name) : AssetObject(name)
{
    public required ShaderId GfxId { get; init; }
    public int Samplers { get; init; }

    public override AssetKind Kind => AssetKind.Shader;
    public override AssetCategory Category => AssetCategory.Graphic;

    internal override AssetObject CopyAndIncreaseGen()
    {
        return new Shader(Name){Id = Id, GId = GId, GfxId = GfxId, Samplers = Samplers, Generation = Generation+1};
    }
}