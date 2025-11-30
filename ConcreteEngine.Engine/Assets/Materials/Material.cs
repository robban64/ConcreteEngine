#region

using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Engine.Assets.Materials;

public sealed class Material
{
    public string TemplateName { get; }
    public string Name { get; }
    public AssetRef<Shader> ShaderRef { get; }
    public MaterialId Id { get; }
    public MaterialState State { get; }
    public MaterialTextureSlots TextureSlots { get; }

    internal Material(MaterialId id, MaterialTemplate template, string name)
    {
        Id = id;
        TemplateName = template.Name;
        Name = name;

        ShaderRef = template.ShaderRef;

        State = new MaterialState(template.Params);
        TextureSlots = new MaterialTextureSlots(template.TextureSlots.AssetSlots);
    }

    public bool Attached => Id > 0;

    public void FillSnapshot(out MaterialParamSnapshot snapshot) =>
        snapshot = new MaterialParamSnapshot(
            color: State.Color,
            specular: State.Specular,
            shininess: State.Shininess,
            uvRepeat: State.UvRepeat,
            transparent: State.Transparency,
            hasNormal: TextureSlots.HasNormalMap,
            hasAlpha: TextureSlots.HasAlphaMap
        );
}