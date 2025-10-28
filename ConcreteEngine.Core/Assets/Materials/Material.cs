#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

public sealed class Material
{
    public string TemplateName { get; }
    public string Name { get; }
    public AssetRef<Shader> ShaderRef { get; }
    public MaterialId Id { get; }
    public MaterialState State { get; }
    public GfxPassState? PassState { get; set; } =  null;
    public GfxPassStateFunc? PassFuncs { get; set; } = null;

    public MaterialTextureSlots TextureSlots { get; }
    
    public bool Attached => Id > 0;

    internal Material(MaterialId id, MaterialTemplate template, string name)
    {
        Id = id;
        TemplateName = template.Name;
        Name = name;

        ShaderRef = template.ShaderRef;

        State = new MaterialState(template.Params);
        TextureSlots = new MaterialTextureSlots(template.TextureSlots.AssetSlots);
    }
    
    public MaterialParams Snapshot() => new(
        Color: State.Color,
        Specular: State.Specular,
        Shininess: State.Shininess,
        UvRepeat: State.UvRepeat,
        HasNormal: TextureSlots.HasNormalMap,
        HasAlpha:  TextureSlots.HasAlphaMap
    );
}