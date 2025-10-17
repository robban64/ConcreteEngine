#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

public sealed class Material
{
    public string TemplateName { get; }
    public string Name { get; }
    public AssetRef<Shader> ShaderRef { get; }
    public MaterialId Id { get; }
    public MaterialState State { get; }
    public MaterialTextureSlots TextureSlots { get; }

    public bool Attached => Id > 0;

    internal Material(MaterialId id, MaterialTemplate template, string name)
    {
        Id = id;
        TemplateName = template.Name;
        Name = name;

        ShaderRef = template.ShaderRef;

        State = new MaterialState(template.Params);
        TextureSlots = new MaterialTextureSlots(template.TextureSlots.Slots);
    }
}