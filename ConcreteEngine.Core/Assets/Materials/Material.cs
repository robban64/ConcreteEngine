#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

public sealed class Material
{
    private readonly TextureId[] _samplerSlots;

    public MaterialId Id { get; }
    public string TemplateName { get; }
    public ShaderId ShaderId { get; set; }
    public Color4 Color { get; set; } = Color4.White;
    public float Shininess { get; set; } = 24f;
    public float SpecularStrength { get; set; } = 0.25f;
    public float UvRepeat { get; set; } = 1;
    public bool Shadows { get; set; } = true;

    public ReadOnlySpan<TextureId> SamplerSlots => _samplerSlots;
    public bool HasNormalMap => SamplerSlots.Length == 2;

    internal Material(MaterialId id, MaterialTemplate template)
    {
        Id = id;
        TemplateName = template.Name;
        ShaderId = template.GfxShaderId;

        _samplerSlots = template.GfxSamplerSlots;
    }
}