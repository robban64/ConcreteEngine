using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Render.Processor;

internal sealed class MaterialProcessor(AssetStore assetStore)
{
    private bool _hasUploadedMaterial;

    internal void SubmitMaterialData(RenderProgram renderer)
    {
        var materials = assetStore.GetAssetList(AssetKind.Material);
        if (materials.DirtyCount == 0 && _hasUploadedMaterial) return;
        if (materials.DirtyCount > 0) _hasUploadedMaterial = false;


        foreach (var id in materials.DirtyIds)
            assetStore.GetUnsafe<Material>(id).Commit();
        
        Submit(renderer.UploadBuffers.Materials);
        
        materials.ClearDirty();
    }

    private void Submit(MaterialBuffer materialBuffer)
    {
        Span<TextureBinding> slots = stackalloc TextureBinding[RenderLimits.TextureSlots];
        foreach (var material in assetStore.GetAssetEnumerator<Material>())
        {
            if(material.BoundShader is not {} shader) continue;

            int slotLength = GetMaterialUploadData(material, shader, slots, out var payload);
            materialBuffer.Submit(in payload, slots.Slice(0, slotLength));
        }

        _hasUploadedMaterial = true;
    }


    private int GetMaterialUploadData(Material material, Shader shader, Span<TextureBinding> slots, out RenderMaterialPayload data)
    {
        material.FillParams(out var param);

        data = new RenderMaterialPayload(material.MaterialId, shader.GfxId, in param, material.RenderProps, material.Pipeline);

        var textureSources = material.GetTextureSources();
        for (var i = 0; i < textureSources.Length; i++)
        {
            var source = textureSources[i];
            if (!ResolveFallbackTextureId(source, out var textureId))
                textureId = assetStore.Get<Texture>(source.AssetTexture).GfxId;

            slots[i] = new TextureBinding(textureId, source.Usage, source.TextureKind);
        }

        return textureSources.Length;
    }

    private static bool ResolveFallbackTextureId(TextureSource source, out TextureId textureId)
    {
        if (source.TextureKind == TextureKind.Texture2DArray)
        {
            textureId = source.OverrideTextureId;
            return true;
        }

        if (source.IsFallback)
        {
            textureId = source.Usage switch
            {
                TextureUsage.Normal => GfxTextures.Fallback.NormalId,
                TextureUsage.Shadowmap => default,
                _ => GfxTextures.Fallback.AlbedoId
            };
            return true;
        }

        if (!source.AssetTexture.IsValid())
        {
            textureId = source.Usage switch
            {
                TextureUsage.Albedo => GfxTextures.Fallback.AlbedoId,
                TextureUsage.Normal => GfxTextures.Fallback.NormalId,
                TextureUsage.Mask => GfxTextures.Fallback.AlphaMaskId,
                _ => default
            };
            return true;
        }

        textureId = default;
        return false;
    }
}