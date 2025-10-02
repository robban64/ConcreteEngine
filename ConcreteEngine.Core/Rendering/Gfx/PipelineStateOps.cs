#region

using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;

#endregion

namespace ConcreteEngine.Core.Rendering.Gfx;

public sealed class PipelineStateOps
{
    private readonly IPrimitiveMeshes _primitiveMeshes;
    private readonly GfxCommands _gfxCmd;
    private readonly GfxTextures _gfxTextures;
    private readonly DrawProcessor _drawProcessor;
    private readonly RenderRegistry _renderRegistry;

    internal PipelineStateOps(GfxContext ctx, DrawProcessor drawProcessor, RenderRegistry renderRegistry)
    {
        _drawProcessor = drawProcessor;
        _renderRegistry = renderRegistry;
        _gfxCmd = ctx.Commands;
        _gfxTextures = ctx.Textures;
        _primitiveMeshes = ctx.Primitives;
    }

    public void BeginScreenPass(in GfxPassClear passClear, in GfxPassState states) =>
        _gfxCmd.BeginScreenPass(in passClear, in states);

    public void BeginRenderPass(FrameBufferId fboId, in GfxPassClear passClear, in GfxPassState states) =>
        _gfxCmd.BeginRenderPass(fboId, in passClear, in states);

    public void EndRenderPass() => _gfxCmd.EndRenderPass();

    public void Blit(FrameBufferId from, FrameBufferId target, bool linear) =>
        _gfxCmd.BlitFramebuffer(from, target, linear);

    public void ClearColor(in GfxPassClear clear) => _gfxCmd.Clear(clear);

    public void ToggleStates(in GfxPassState states) => _gfxCmd.ApplyState(states);

    public void GenerateMips(TextureId textureId) => _gfxTextures.GenerateMipMaps(textureId);
    
    public void DrawFullscreenQuad(ShaderId shaderId, IReadOnlyList<TextureId> sources)
    {
        UseShader(shaderId);

        for (int i = 0; i < sources.Count; i++)
            _gfxCmd.BindTexture(sources[i], i);

        DrawFsq();
    }

    public void DrawFullscreenQuad(ShaderId shaderId, ReadOnlySpan<TextureId> sources)
    {
        UseShader(shaderId);

        for (int i = 0; i < sources.Length; i++)
            _gfxCmd.BindTexture(sources[i], i);

        DrawFsq();
    }

    private void DrawFsq()
    {
        _gfxCmd.BindMesh(_primitiveMeshes.FsqQuad);
        _gfxCmd.DrawBoundMesh(_primitiveMeshes.FsqQuad, 0);
    }
    
    private void UseShader(ShaderId shaderId)
    {
        var renderShader = _renderRegistry.GetRenderShader(shaderId);
        _gfxCmd.UseShader(shaderId, renderShader.Locations);
    }
}