using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Assets;

public sealed class Material
{
    public MaterialId Id { get; }

    public string Name { get; }

    public AssetId TemplateId { get; }
    public AssetId AssetShader { get; }
    public MaterialState State { get; }
    public MaterialTextureSlots TextureSlots { get; }


    internal Material(MaterialId id, MaterialTemplate template, string name)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(id.Id, 1, nameof(id));
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(name);

        Id = id;
        TemplateId = template.Id;
        Name = name;
        AssetShader = template.AssetShader;

        State = new MaterialState(template.Params) { Id = id };
        TextureSlots = new MaterialTextureSlots(template.TextureSlots.AssetSlots);
    }
    

    public void FillSnapshot(out RenderMaterial snapshot) =>
        snapshot = new RenderMaterial(
            color: State.Color,
            specular: State.Specular,
            shininess: State.Shininess,
            uvRepeat: State.UvRepeat,
            transparent: State.Transparency,
            hasNormal: TextureSlots.HasNormalMap,
            hasAlpha: TextureSlots.HasAlphaMap
        );

    public MaterialMeta GetMeta()
    {
        var transparent = State.Transparency;
        var hasNormal = TextureSlots.HasNormalMap;
        var hasAlpha = TextureSlots.HasAlphaMap;
        return new MaterialMeta(Id, transparent, hasNormal, hasAlpha);
    }
}