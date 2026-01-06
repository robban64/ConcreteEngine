using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Metadata.Asset;

namespace ConcreteEngine.Engine.Assets;

public sealed class MaterialTemplate : AssetObject
{
    public override AssetKind Kind => AssetKind.Material;
    public override AssetCategory Category => AssetCategory.Renderer;

    public required AssetRef<Shader> ShaderRef { get; init; }

    public required MaterialState Params { get; init; }

    public MaterialTextureSlots TextureSlots { get; }


    internal MaterialTemplate(AssetTextureSlot[] samplerSlots)
    {
        TextureSlots = new MaterialTextureSlots(samplerSlots);
    }
}