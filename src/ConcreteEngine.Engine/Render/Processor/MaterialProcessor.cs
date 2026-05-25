using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Handles;
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

        materials.ClearDirty();

        var materialBuffer = renderer.UploadBuffers.Materials;

        Span<TextureBinding> slots = stackalloc TextureBinding[RenderLimits.TextureSlots];
        foreach (var material in assetStore.GetAssetEnumerator<Material>())
        {
            int slotLength = GetMaterialUploadData(material, slots, out var payload);
            materialBuffer.Submit(in payload, slots.Slice(0, slotLength));
        }

        _hasUploadedMaterial = true;
    }


    private int GetMaterialUploadData(Material material, Span<TextureBinding> slots, out RenderMaterialPayload data)
    {
        var shader = assetStore.Get<Shader>(material.ShaderId).GfxId;

        material.FillParams(out var param);

        data = new RenderMaterialPayload(material.MaterialId, shader, in param,
            material.GetProperties(), material.Pipeline);

        var textureSources = material.GetTextureSources();
        for (var i = 0; i < textureSources.Length; i++)
        {
            var source = textureSources[i];
            if(!ResolveFallbackTextureId(source, out var textureId))
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
            textureId = GfxTextures.Fallback.AlbedoId;
            if (source.Usage == TextureUsage.Normal) textureId = GfxTextures.Fallback.NormalId;
            return true;
        }

        if (source.Usage == TextureUsage.Shadowmap)
        {
            textureId = default;
            return true;
        }

        if (!source.AssetTexture.IsValid())
        {
            switch (source.Usage)
            {
                case TextureUsage.Albedo: 
                    textureId = GfxTextures.Fallback.AlbedoId;
                    return true;
                case TextureUsage.Normal: 
                    textureId = GfxTextures.Fallback.NormalId;
                    return true;
                case TextureUsage.Mask: 
                    textureId = GfxTextures.Fallback.AlphaMaskId;
                    return true;
            }
        }

        textureId = default;
        return false;
    }

}