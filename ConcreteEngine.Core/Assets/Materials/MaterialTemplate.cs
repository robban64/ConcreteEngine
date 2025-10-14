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
    public required AssetId ShaderAssetId { get; init; }
    public AssetId[] TextureAssetIds { get; init; } = Array.Empty<AssetId>();
    public AssetId? CubeMapAssetId { get; init; } = null;

    public Vector4 Color { get; set; } = Vector4.One;

    public override AssetKind Kind => AssetKind.Material;
    public override AssetCategory Category => AssetCategory.Graphic;


    private TextureId[] _gfxSamplerSlots = null!;
    internal TextureId[] GfxSamplerSlots => _gfxSamplerSlots;

    public ShaderId GfxShaderId { get; private set; }

    internal MaterialTemplate()
    {
    }

    internal void Initialize(IAssetStore store)
    {
        var shader = store.Get<Shader>(ShaderAssetId);
        GfxShaderId = shader.ResourceId;

        _gfxSamplerSlots = new TextureId[shader.Samplers];
        if (CubeMapAssetId is { } cubeMapAssetId)
        {
            _gfxSamplerSlots[0] = store.Get<CubeMap>(cubeMapAssetId).ResourceId;
            return;
        }
        
        for (int i = 0; i < shader.Samplers; i++)
        {
            if (i < TextureAssetIds.Length)
                _gfxSamplerSlots[i] = store.Get<Texture2D>(TextureAssetIds[i]).ResourceId;
            else
                _gfxSamplerSlots[i] = default;
        }
    }
}