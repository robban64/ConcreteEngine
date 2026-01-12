using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed record MaterialTemplate : AssetObject
{
    public override AssetKind Kind => AssetKind.Material;
    public override AssetCategory Category => AssetCategory.Renderer;

    public required AssetId AssetShader { get; init; }

    public required MaterialTemplateParams Params { get; init; }

    public MaterialTextureSlots TextureSlots { get; init; }


    public MaterialTemplate(AssetTextureSlot[] samplerSlots)
    {
        TextureSlots = new MaterialTextureSlots(samplerSlots);
    }
}

public sealed class MaterialTemplateParams
{
    public Color4? Color { get; init; }
    public float? Shininess { get; init; }
    public float? Specular { get; init; }
    public float? UvRepeat { get; init; }
}