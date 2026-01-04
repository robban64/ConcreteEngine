using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Materials;

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