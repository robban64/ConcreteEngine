#region

using System.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

public sealed class MaterialTemplate : AssetObject
{
    public required Shader Shader { get; init; }
    public Texture2D[] Textures { get; init; } = Array.Empty<Texture2D>();
    public CubeMap? CubeMap { get; init; } = null;

    public Vector4 Color { get; set; } = Vector4.One;

    public override AssetKind Kind => AssetKind.Material;
    public override AssetCategory Category => AssetCategory.Graphic;


    private TextureId[] _samplerSlots = null!;
    internal TextureId[] SamplerSlots => _samplerSlots;

    internal MaterialTemplate()
    {
    }

    internal void Initialize()
    {
        _samplerSlots = new TextureId[Shader.Samplers];
        for (int i = 0; i < Shader.Samplers; i++)
            _samplerSlots[i] = i < Textures.Length ? Textures[i].ResourceId : default;
    }
}