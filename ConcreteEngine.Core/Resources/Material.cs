#region

using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Resources;


public sealed class Material
{
    private readonly Dictionary<ShaderUniform, IMaterialValue> _values;
    private readonly TextureId[] _samplerSlots = [];

    public MaterialId Id { get; }
    public string TemplateName { get; }
    public ShaderId ShaderId { get; set; }
    
    public CubeMap? CubeMap { get;  }

    public TextureId[] SamplerSlots => _samplerSlots;


    public Color4 Color { get; set; } = Color4.White;
    public float Shininess { get; set; } = 12;
    public float SpecularStrength { get; set; } = 1;
    public float UvRepeat { get; set; } = 1;


    internal Material(MaterialId id, MaterialTemplate template)
    {
        Id = id;
        TemplateName = template.Name;
        ShaderId = template.Shader.ResourceId;
        CubeMap = template.CubeMap;

        _values = new Dictionary<ShaderUniform, IMaterialValue>(4);
        
        if (template.Shader.Samplers == 0)
            return;

        if (CubeMap != null)
        {
            _samplerSlots = new TextureId[1];
            _samplerSlots[0] = CubeMap.ResourceId;
        }
        else
        {
            _samplerSlots = new TextureId[template.Shader.Samplers];
            if (template.Textures?.Length > 0)
            {
                int length = int.Min((int)template.Shader.Samplers, template.Textures.Length);
                for (int i = 0; i < length; i++)
                    SamplerSlots[i] = template.Textures[i].ResourceId;
            }
        }
    }
}