using System.Numerics;
using System.Runtime.CompilerServices;
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
        avg.BeginSample();
        Submit();
        avg.EndSample();
        avg.ResetAndPrint("Materials: ");
        _materialStore.ClearDirty();
    }

    private AvgFrameTimer avg;

    private void Submit()
    {
        Span<TextureBinding> slots = stackalloc TextureBinding[RenderLimits.TextureSlots];
        Shader lastShader = null!;
        var lastProfile = MaterialProfileId.None;
        foreach (var id in _materialStore.GetDirtySpan())
        {
            var material = AssetManager.AssetStore.GetUnsafe<Material>(id);
            var flag = material.Commit();
            if ((flag & AssetDirtyFlag.State) == 0 && (flag & AssetDirtyFlag.Structure) == 0) continue;

            if (lastProfile == MaterialProfileId.None || material.ProfileId != lastProfile)
            {
                lastProfile = material.ProfileId;
                lastShader = material.BoundShader;
            }

            FillSamplers(material, slots);
            SubmitUniform(material.State, lastShader);
        }
    }

    private void SubmitUniform(MaterialState state, Shader shader)
    {
        ref var uniform = ref _materialBuffer.Submit(
            state.MaterialId,
            new RenderMaterialMeta(
                shader.GfxId,
                state.DrawState,
                state.DrawFunctions,
                state.ReceiveShadows ? shader.DefaultBindings.ShadowMapBinding : (sbyte)-1
            ));

        uniform.MatColor = state.Color;
        uniform.MatParams0.X = state.Shininess;
        uniform.MatParams0.Y = state.Roughness;
        uniform.MatParams0.W = state.Metallic;
            
        uniform.MatParams1.X = state.SpecularColor.A;
        uniform.MatParams1.Y = state.UvTransform.W;

        var cutoff = state.IsTransparent ? (state.HasAlphaMask ? 0.5f : 0.1f) : 0f;
        uniform.MatParams1.Z = cutoff;
        uniform.MatParams1.W = state.HasAlphaMask ? 1f : 0f;
    }

    private void FillSamplers(Material material, Span<TextureBinding> slots)
    {
        var textureSources = material.GetSourceSpan();
        for (var i = 0; i < textureSources.Length; i++)
        {
            var source = textureSources[i];
            var textureId = source.FallbackTexture;
            if(source.OverrideTexture > 0) textureId = source.OverrideTexture;
            else if (source.AssetTexture.Value > 0)
                textureId = AssetManager.AssetStore.Get<Texture>(source.AssetTexture).GfxId;
            
            slots[i] = new TextureBinding(textureId, source.Usage, (byte)i);
        }

        _materialBuffer.SubmitBindings(material.MaterialId, slots.Slice(0, textureSources.Length));
    }
}