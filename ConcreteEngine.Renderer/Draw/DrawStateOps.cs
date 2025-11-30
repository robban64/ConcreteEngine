#region

using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Registry;
using ConcreteEngine.Renderer.State;

#endregion

namespace ConcreteEngine.Renderer.Draw;

public sealed class DrawStateOps
{
    private readonly GfxCommands _gfxCmd;
    private readonly GfxTextures _gfxTextures;
    private readonly RenderRegistry _renderRegistry;
    private readonly RenderView _renderView;
    private readonly RenderParamsSnapshot _paramsSnapshot;
    private readonly DrawBuffers _drawBuffers;

    private readonly DrawStateContext _ctx;

    internal DrawStateOps(DrawStateContext ctx, DrawStateContextPayload ctxPayload, DrawBuffers drawBuffers)
    {
        _renderRegistry = ctxPayload.Registry;
        _renderView = ctxPayload.RenderView;
        _paramsSnapshot = ctxPayload.Snapshot;
        _drawBuffers = drawBuffers;
        _gfxCmd = ctxPayload.Gfx.Commands;
        _gfxTextures = ctxPayload.Gfx.Textures;

        _ctx = ctx;
    }

    public void ActivateDepthMode()
    {
        _ctx.SetDepthMode();

        _renderView.ApplyLightViewOverride(_paramsSnapshot.DirLight.Direction, _paramsSnapshot);
        _drawBuffers.UploadShadow(in _renderView.ProjectionViewMatrix);
        _drawBuffers.UploadCameraView(_renderView);
    }

    public void RestoreMode()
    {
        _ctx.ResetPassMode();
        _renderView.ClearOverride();
        _drawBuffers.UploadCameraView(_renderView);
    }


    public void ApplyStateFunctions(GfxPassStateFunc passFunc)
    {
        _gfxCmd.ApplyStateFunctions(passFunc);
        _ctx.PassStateFunc = passFunc;
    }

    public void BeginScreenPass(in GfxPassClear passClear, GfxPassState states)
    {
        _gfxCmd.BeginScreenPass(in passClear, states);
        _ctx.PassState = states;
    }

    public void BeginRenderPass(FrameBufferId fboId, in GfxPassClear passClear, GfxPassState states)
    {
        _gfxCmd.BeginRenderPass(fboId, in passClear, states);
        _ctx.PassState = states;
    }

    public void ContinueFromRenderPass(FrameBufferId fboId, GfxPassState states)
    {
        _gfxCmd.BindFramebuffer(fboId);
        _gfxCmd.ApplyState(states);
        _ctx.PassState = states;
    }

    public void EndRenderPass() => _gfxCmd.EndRenderPass();

    public void Blit(FrameBufferId from, FrameBufferId target, bool linear) =>
        _gfxCmd.BlitFramebuffer(from, target, linear);

    public void ClearColor(in GfxPassClear clear) => _gfxCmd.Clear(clear);

    public void ToggleStates(GfxPassState states) => _gfxCmd.ApplyState(states);

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
        _gfxCmd.BindMesh(_ctx.FsqMesh);
        _gfxCmd.DrawMesh(_ctx.FsqMesh, 0);
    }

    private void UseShader(ShaderId shaderId)
    {
        //var renderShader = _renderRegistry.GetRenderShader(shaderId);
        _gfxCmd.UseShader(shaderId);
    }
}