#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Resources;

public sealed class Material
{
    private readonly TextureId[] _samplerSlots;

    public MaterialId Id { get; }
    public string TemplateName { get; }
    public ShaderId ShaderId { get; set; }

    public CubeMap? CubeMap { get; }

    public TextureId[] SamplerSlots => _samplerSlots;


    public Color4 Color { get; set; } = Color4.White;
    public float Shininess { get; set; } = 24f;
    public float SpecularStrength { get; set; } = 0.25f;
    public float UvRepeat { get; set; } = 1;


    public bool HasNormalMap => SamplerSlots.Length == 2;

    internal Material(MaterialId id, MaterialTemplate template)
    {
        Id = id;
        TemplateName = template.Name;
        ShaderId = template.Shader.ResourceId;
        CubeMap = template.CubeMap;

        if (template.Shader.Samplers == 0)
            return;

        if (CubeMap != null)
        {
            _samplerSlots = new TextureId[1];
            _samplerSlots[0] = CubeMap.ResourceId;
        }
        else
        {
            _samplerSlots = template.SamplerSlots;
        }
    }
}