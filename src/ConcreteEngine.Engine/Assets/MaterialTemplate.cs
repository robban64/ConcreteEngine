using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;

namespace ConcreteEngine.Engine.Assets;

public sealed record MaterialTemplate : AssetObject
{
    public override AssetKind Kind => AssetKind.Material;
    public override AssetCategory Category => AssetCategory.Renderer;

    public required AssetRef<Shader> ShaderRef { get; init; }

    public required MaterialState Params { get; init; }

    public MaterialTextureSlots TextureSlots { get; init; }


    internal MaterialTemplate(AssetTextureSlot[] samplerSlots)
    {
        TextureSlots = new MaterialTextureSlots(samplerSlots);
    }
}