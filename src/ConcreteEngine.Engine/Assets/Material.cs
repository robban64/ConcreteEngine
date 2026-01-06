using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Assets;

public sealed class Material
{
    public MaterialId Id { get; }
    public string TemplateName { get; }
    public string Name { get; }
    public AssetRef<Shader> AssetShader { get; }
    public MaterialState State { get; }
    public MaterialTextureSlots TextureSlots { get; }

    public bool IsAssetMaterial { get; }

    internal Material(MaterialId id, MaterialTemplate template, string name)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(id.Id, 1, nameof(id));
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(name);

        Id = id;
        TemplateName = template.Name;
        Name = name;
        AssetShader = template.ShaderRef;
        IsAssetMaterial = TemplateName.Length >= 1;

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
        return new MaterialMeta(Id, transparent, hasNormal, hasAlpha, IsAssetMaterial);
    }
}