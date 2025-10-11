using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Registry;

public readonly record struct MaterialParams(Color4 Color, float Specular, float Shininess, float UvRepeat);

internal delegate void MaterialApplyDel();

public sealed class RenderMaterial
{
    public MaterialId Id { get; }
    public ShaderId ShaderId { get; set; }
    
    public TextureId BaseTexture { get; set; }
    
    public TextureId NormalMap { get; set; }

    private readonly TextureId[] _samplerSlots;

    private MaterialParams _materialParams;

    internal RenderMaterial(MaterialId id, ShaderId shaderId, int samplerSlots)
    {
        Id = id;
        ShaderId = shaderId;
        _samplerSlots = new TextureId[samplerSlots];
    }
}

public sealed class RenderMaterialProps
{
}

