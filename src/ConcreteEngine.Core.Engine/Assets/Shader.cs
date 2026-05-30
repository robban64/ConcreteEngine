using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class Shader(string name) : AssetObject(name)
{
    public required ShaderId GfxId { get; init; }

    public required UniformSamplerInfo[] Samplers;

    public override AssetKind Kind => AssetKind.Shader;
    public override AssetCategory Category => AssetCategory.Graphic;
}