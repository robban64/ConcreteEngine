using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx;
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
        if (materials.DirtyCount >  0) _hasUploadedMaterial = false;

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

        var textureSlots = material.GetTextureSources();
        for (var i = 0; i < textureSlots.Length; i++)
        {
            var slot = textureSlots[i];
            var textureId = ResolveTextureId(slot);
            slots[i] = new TextureBinding(textureId, slot.Usage, slot.TextureKind);
        }

        return textureSlots.Length;
    }

    private TextureId ResolveTextureId(TextureSource source)
    {
        if (source.IsFallback)
        {
            var texId = GfxTextures.Fallback.AlbedoId;
            if (source.Usage == TextureUsage.Normal) texId = GfxTextures.Fallback.NormalId;
            return texId;
        }

        if (source.Usage == TextureUsage.Shadowmap) return default;

        if (!source.Texture.IsValid())
        {
            switch (source.Usage)
            {
                case TextureUsage.Albedo: return GfxTextures.Fallback.AlbedoId;
                case TextureUsage.Normal: return GfxTextures.Fallback.NormalId;
                case TextureUsage.Mask: return GfxTextures.Fallback.AlphaMaskId;
            }
        }

        return assetStore.Get<Texture>(source.Texture).GfxId;
    }
}