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
    public AssetRef<Shader> ShaderRef { get;  } 
    public MaterialId Id { get; private set; }
    public MaterialTemplateParams Parameters { get; }
    public MaterialTextureSlots TextureSlots { get; }

    public bool Attached => Id > 0;

    internal Material(MaterialTemplate template, string name)
    {
        TemplateName = template.Name;
        Name = name;

        ShaderRef = template.ShaderRef;

        Parameters = new MaterialTemplateParams(template.Params);
        TextureSlots = new MaterialTextureSlots(template.TextureSlots.Slots);
    }

    internal void Attach(MaterialId id)
    {
        if(Attached) throw new InvalidOperationException("Material already attached");
        Id = id;
    }
}