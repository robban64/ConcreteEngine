#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics;
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
    public TextureId[] SamplerSlots => _samplerSlots;


    internal Material(MaterialId id, MaterialTemplate template)
    {
        Id = id;
        TemplateName = template.Name;
        ShaderId = template.Shader.ResourceId;

        _values = new Dictionary<ShaderUniform, IMaterialValue>(4);

        if (template.Shader.Samplers > 0)
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