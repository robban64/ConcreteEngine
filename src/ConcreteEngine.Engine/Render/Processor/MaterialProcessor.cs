using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Scene;
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
        CommitMaterials();
        Submit();
        _materialStore.ClearDirty();
    }

    private void CommitMaterials()
    {
        var assetStore = AssetStore.Instance;
        foreach (var id in _materialStore.GetDirtySpan())
        {
            assetStore.GetUnsafe<Material>(id).Commit();
        }
    }


    private void Submit()
    {
        var assetStore = AssetStore.Instance;
        var materialBuffer = _materialBuffer;

        Span<TextureBinding> slots = stackalloc TextureBinding[RenderLimits.TextureSlots];
        foreach (var id in _materialStore.GetDirtySpan())
        {
            var material = assetStore.GetUnsafe<Material>(id);
            var toggles = FillSamplers(slots, material, assetStore, materialBuffer);
            SubmitUniform(material, materialBuffer, toggles);
        }
    }

    private static void SubmitUniform(Material material, MaterialBuffer buffer, MaterialRenderToggles toggles)
    {
        var state = material.State;

        if (material.BoundShader is not { } shader)
            shader = Shader.FallbackShader;

        ref var uniform = ref buffer.Submit(
            material.MaterialId,
            new RenderMaterialMeta(
                shader.GfxId,
                state.DrawState,
                state.PassFunctions,
                shader.DefaultBindings.ShadowMapBinding
            ));

        uniform.MatColor = state.Color;
        uniform.MatParams0 = new Vector4(state.Specular, state.UvRepeat, 1.0f, 1.0f);
        uniform.MatParams1 = new Vector4(
            state.Shininess,
            toggles.HasNormal ? 1f : 0f,
            state.Transparency ? 1f : 0f,
            toggles.HasAlphaMask ? 1f : 0f
        );
    }

    private static MaterialRenderToggles FillSamplers(Span<TextureBinding> slots, Material material,
        AssetStore assetStore, MaterialBuffer buffer)
    {
        var toggles = new MaterialRenderToggles();
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