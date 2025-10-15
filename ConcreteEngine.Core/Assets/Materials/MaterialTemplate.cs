#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

public sealed class MaterialTemplate : AssetObject
{
    public override AssetKind Kind => AssetKind.Material;
    public override AssetCategory Category => AssetCategory.Renderer;

    public required AssetRef<Shader> ShaderAssetId { get; init; }
    public AssetRef<Texture2D>[] TextureAssetIds { get; init; } = Array.Empty<AssetRef<Texture2D>>();
    public AssetRef<CubeMap>? CubeMapAssetId { get; init; } = null;
    
    
    private TextureId[] _samplerSlots = null!;
    public ShaderId BoundShaderId { get; private set; }
    
    public MaterialTemplateParams Params { get; init; }
    
    internal MaterialTemplate()
    {
    }

    public ReadOnlySpan<TextureId> SamplerSlots => _samplerSlots;

    internal void Initialize(IAssetStore store)
    {
        var shader = store.GetByRef(ShaderAssetId);
        BoundShaderId = shader.ResourceId;

        _samplerSlots = new TextureId[shader.Samplers];
        if (CubeMapAssetId is { } cubeMapAssetId)
        {
            _samplerSlots[0] = store.GetByRef(cubeMapAssetId).ResourceId;
            return;
        }

        for (int i = 0; i < shader.Samplers; i++)
        {
            if (i < TextureAssetIds.Length)
                _samplerSlots[i] = store.GetByRef(TextureAssetIds[i]).ResourceId;
            else
                _samplerSlots[i] = default;
        }
    }
}