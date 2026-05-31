using System.Numerics;
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
            if (material.BoundShader is not { } shader) continue;

            var textureSources = material.GetTextureSources();
            for (var i = 0; i < textureSources.Length; i++)
            {
                var source = textureSources[i];
                if (!ResolveFallbackTextureId(source, out var textureId))
                    textureId = assetStore.Get<Texture>(source.AssetTexture).GfxLink.GfxId;

                slots[i] = new TextureBinding(textureId, source.Usage, (sbyte)i);
            }

            var props = material.RenderProps;
            float transparency = props.HasTransparency ? 1f : 0f;
            float normal = props.HasNormal ? 1f : 0f;
            float alpha = props.HasAlphaMask ? 1f : 0f;

            var meta = new RenderMaterialMeta(
                material.MaterialId, 
                shader.GfxId, 
                material.Pipeline.DrawState,
                material.Pipeline.PassFunctions, 
                shader.DefaultBindings.ShadowMapBinding
            );
            
            ref var uniform = ref materialBuffer.Submit(in meta, slots.Slice(0, textureSources.Length));

            uniform.MatColor = material.Color;
            uniform.MatParams0 = new Vector4(material.Specular, material.UvRepeat, 1.0f, 1.0f);
            uniform.MatParams1 = new Vector4(material.Shininess, normal, transparency, alpha);
        }

        _hasUploadedMaterial = true;
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