using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Render.Processor;

internal sealed class MaterialProcessor
{
    private bool _hasUploadedMaterial;

    internal void SubmitMaterialData(RenderProgram renderer)
    {
        var assetStore = AssetStore.Instance;
        
        var materials = assetStore.GetAssetList(AssetKind.Material);
        if (materials.DirtyCount == 0 && _hasUploadedMaterial) return;
        if (materials.DirtyCount > 0) _hasUploadedMaterial = false;

        foreach (var id in materials.GetDirtySpan())
            assetStore.GetUnsafe<Material>(id).Commit();

        Submit(assetStore, renderer.UploadBuffers.Materials);

        materials.ClearDirty();
    }

    private void Submit(AssetStore assetStore, MaterialBuffer materialBuffer)
    {
        Span<TextureBinding> slots = stackalloc TextureBinding[RenderLimits.TextureSlots];
        foreach (var material in assetStore.GetAssetEnumerator<Material>())
        {
            material.ClearDirty();
            
            if (material.BoundShader is not { } shader) continue;

            var textureSources = material.GetTextureSources();
            for (var i = 0; i < textureSources.Length; i++)
            {
                var source = textureSources[i];
                if (!ResolveFallbackTextureId(source, out var textureId))
                    textureId = assetStore.Get<Texture>(source.AssetTexture).GfxId;

                slots[i] = new TextureBinding(textureId, source.Usage, (sbyte)i);
            }

            var props = material.RenderToggles;
            float transparency = props.HasTransparency ? 1f : 0f;
            float normal = props.HasNormal ? 1f : 0f;
            float alpha = props.HasAlphaMask ? 1f : 0f;

            var state = material.State;
            var meta = new RenderMaterialMeta(
                material.MaterialId,
                shader.GfxId,
                state.DrawState,
                state.PassFunctions,
                shader.DefaultBindings.ShadowMapBinding
            );

            ref var uniform = ref materialBuffer.Submit(in meta, slots.Slice(0, textureSources.Length));

            uniform.MatColor = state.Color;
            uniform.MatParams0 = new Vector4(state.Specular, state.UvRepeat, 1.0f, 1.0f);
            uniform.MatParams1 = new Vector4(state.Shininess, normal, transparency, alpha);
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