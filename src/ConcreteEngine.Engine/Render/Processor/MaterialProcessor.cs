using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Render.Processor;

internal sealed class MaterialProcessor(RenderProgram renderProgram)
{
    private readonly MaterialBuffer _materialBuffer = renderProgram.UploadBuffers.Materials;
    private readonly AssetTypeStore _materialStore = AssetStore.Instance.GetTypeStore(AssetKind.Material);

    internal void Commit()
    {
        if (_materialStore.DirtyCount == 0) return;
        Submit();
        _materialStore.ClearDirty();
    }

    private void Submit()
    {
        var assetStore = AssetStore.Instance;

        Span<TextureBinding> slots = stackalloc TextureBinding[RenderLimits.TextureSlots];
        foreach (var id in _materialStore.GetDirtySpan())
        {
            var material = assetStore.GetUnsafe<Material>(id);
            var flag = material.Commit();
            if ((flag & AssetDirtyFlag.State) == 0 && (flag & AssetDirtyFlag.Structure) == 0) continue;
            var toggles = FillSamplers(slots, material.State, assetStore, _materialBuffer);
            SubmitUniform(material, _materialBuffer, toggles);
        }
    }

    private static void SubmitUniform(Material material, MaterialBuffer buffer, MaterialRenderToggles toggles)
    {
        var state = material.State;

        ref var uniform = ref buffer.Submit(
            state.MaterialId,
            new RenderMaterialMeta(
                material.BoundShader.GfxId,
                state.DrawState,
                state.PassFunctions,
                material.BoundShader.DefaultBindings.ShadowMapBinding
            ));

        state.FillParams(out var param);
        uniform.MatColor = param.Color;
        uniform.MatParams0 = new Vector4(param.Specular, param.UvRepeat, 1.0f, 1.0f);
        uniform.MatParams1 = new Vector4(
            param.Shininess,
            toggles.HasNormal ? 1f : 0f,
            toggles.HasTransparency ? 1f : 0f,
            toggles.HasAlphaMask ? 1f : 0f
        );
    }

    private static MaterialRenderToggles FillSamplers(Span<TextureBinding> slots, MaterialState material,
        AssetStore assetStore, MaterialBuffer buffer)
    {
        MaterialRenderToggles toggles = default;
        toggles.HasTransparency = material.Transparency;
        var textureSources = material.GetTextureSources();
        for (var i = 0; i < textureSources.Length; i++)
        {
            var source = textureSources[i];
            if (!ResolveFallbackTextureId(source, out var textureId))
            {
                textureId = assetStore.Get<Texture>(source.AssetTexture).GfxId;
                if (source.Usage == TextureUsage.Normal) toggles.HasNormal = true;
                else if (source.Usage == TextureUsage.Mask) toggles.HasAlphaMask = true;
            }

            slots[i] = new TextureBinding(textureId, source.Usage, (byte)i);
        }

        buffer.SubmitBindings(material.MaterialId, slots.Slice(0, textureSources.Length));
        return toggles;
    }

    private static bool ResolveFallbackTextureId(TextureSource source, out TextureId textureId)
    {
        if (source.OverrideTextureId.IsValid())
        {
            textureId = source.OverrideTextureId;
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