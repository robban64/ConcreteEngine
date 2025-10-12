#region

using System.Numerics;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Resources;

public sealed class MaterialTemplate : IAssetFile
{
    public required string Name { get; init; }
    public required Shader Shader { get; init; }
    public Texture2D[] Textures { get; init; } = Array.Empty<Texture2D>();
    public CubeMap? CubeMap { get; init; } = null;

    public Vector4 Color { get; set; } = Vector4.One;
    public AssetKind AssetType => AssetKind.Material;

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