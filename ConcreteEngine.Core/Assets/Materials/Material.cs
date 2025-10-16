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

    public MaterialId Id { get; private set; }
    public AssetRef<Shader> ShaderRef { get;  } 
    public MaterialTemplateParams Parameters { get; }
    public MaterialTextureSlots TextureSlots { get; }
    
    public ShaderId ShaderId { get; set; }


    internal Material(MaterialTemplate template)
    {
        TemplateName = template.Name;

        ShaderRef = template.ShaderRef;

        Parameters = new MaterialTemplateParams();
        Parameters.Set(template.Params.GetDataParams());

        TextureSlots = new MaterialTextureSlots(template.TextureSlots.Slots);
    }

    internal void Attach(MaterialId id)
    {
        Id = id;
        ShaderId = template.BoundShaderId;
    }
}