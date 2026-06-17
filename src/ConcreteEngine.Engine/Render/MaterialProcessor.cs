using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Render;

internal sealed class MaterialProcessor(RenderProgram renderProgram)
{
    private readonly MaterialBuffer _materialBuffer = renderProgram.UploadBuffers.Materials;
    private readonly AssetTypeStore _materialStore = AssetManager.AssetStore.GetTypeStore(AssetKind.Material);

    internal void Commit()
    {
        if (_materialStore.DirtyCount == 0) return;
        Submit();
        _materialStore.ClearDirty();
    }


    private void Submit()
    {
        Span<TextureBinding> slots = stackalloc TextureBinding[RenderLimits.TextureSlots];
        Shader lastShader = null!;
        var lastProfile = MaterialProfile.None;
        foreach (var id in _materialStore.GetDirtySpan())
        {
            var material = AssetManager.AssetStore.GetUnsafe<Material>(id);
            var flag = material.Commit();
            if ((flag & AssetDirtyFlag.State) == 0 && (flag & AssetDirtyFlag.Structure) == 0) continue;

            if (lastProfile == MaterialProfile.None || material.Profile != lastProfile)
            {
                lastProfile = material.Profile;
                lastShader = material.BoundShader;
            }
            var toggles = FillSamplers(material, slots);
            SubmitUniform(material.State, lastShader, toggles);
        }
    }

    private  void SubmitUniform(MaterialState state, Shader shader, MaterialRenderToggles toggles)
    {
        ref var uniform = ref _materialBuffer.Submit(
            state.MaterialId,
            new RenderMaterialMeta(
                shader.GfxId,
                state.DrawState,
                state.DrawFunctions,
                shader.DefaultBindings.ShadowMapBinding
            ));

        uniform.MatColor = state.Albedo;
        uniform.MatParams0 = new Vector4(state.SpecularColor.A, state.UvTransform.W, 1.0f, 1.0f);
        uniform.MatParams1 = new Vector4(
            state.Shininess,
            toggles.HasNormal ? 1f : 0f,
            toggles.HasTransparency ? 1f : 0f,
            toggles.HasAlphaMask ? 1f : 0f
        );
    }

    private  MaterialRenderToggles FillSamplers(Material material, Span<TextureBinding> slots)
    {
        MaterialRenderToggles toggles = default;
        toggles.HasTransparency = material.State.Transparency;
        var textureSources = material.GetSourceSpan();
        for (var i = 0; i < textureSources.Length; i++)
        {
            var source = textureSources[i];
            if (!ResolveFallbackTextureId(source, out var textureId))
            {
                textureId = AssetManager.AssetStore.Get<Texture>(source.AssetTexture).GfxId;
                if (source.Usage == TextureUsage.Normal) toggles.HasNormal = true;
                else if (source.Usage == TextureUsage.Mask) toggles.HasAlphaMask = true;
            }

            slots[i] = new TextureBinding(textureId, source.Usage, (byte)i);
        }

        _materialBuffer.SubmitBindings(material.MaterialId, slots.Slice(0, textureSources.Length));
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