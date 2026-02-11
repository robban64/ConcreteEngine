using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Renderer.Draw;

public sealed class DrawStateOps
{
    private readonly GfxCommands _gfxCmd;
    private readonly GfxTextures _gfxTextures;
    private readonly RenderCamera _renderCamera;
    private readonly DrawBuffers _drawBuffers;

    private readonly DrawStateContext _ctx;

    internal DrawStateOps(DrawStateContext ctx, DrawStateContextPayload ctxPayload, DrawBuffers drawBuffers)
    {
        _renderCamera = ctxPayload.RenderCamera;
        _drawBuffers = drawBuffers;
        _gfxCmd = ctxPayload.Gfx.Commands;
        _gfxTextures = ctxPayload.Gfx.Textures;

        _ctx = ctx;
    }

    public void ActivateDepthMode()
    {
        _ctx.SetDepthMode();

        _renderCamera.ToggleLightView();
        _drawBuffers.UploadShadow(in _renderCamera.LightSpace.ProjectionViewMatrix);
        _drawBuffers.UploadCameraView(_renderCamera);
    }

    public void RestoreMode()
    {
        _ctx.ResetPassMode();
        _renderCamera.RestoreView();
        _drawBuffers.UploadCameraView(_renderCamera);
    }

    public void ApplyStateFunctions(GfxPassFunctions passFunc)
    {
        _gfxCmd.ApplyStateFunctions(passFunc);
        _ctx.PassFunctions = passFunc;
    }

    public void BeginScreenPass(GfxPassClear passClear, GfxPassState states)
    {
        _gfxCmd.BeginScreenPass(passClear, states);
        _ctx.PassState = states;
    }

    public void BeginRenderPass(FrameBufferId fboId, GfxPassClear passClear, GfxPassState states)
    {
        _gfxCmd.BeginRenderPass(fboId, passClear, states);
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

    public void ClearColor(GfxPassClear clear) => _gfxCmd.Clear(clear);

    public void ToggleStates(GfxPassState states) => _gfxCmd.ApplyState(states);

    public void GenerateMips(TextureId textureId) => _gfxTextures.GenerateMipMaps(textureId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawFullscreenQuad(ShaderId shaderId, ReadOnlySpan<TextureId> sources)
    {
        UseShader(shaderId);

        for (var i = 0; i < sources.Length; i++)
            _gfxCmd.BindTexture(sources[i], i);

        DrawFsq();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawFsq()
    {
        _gfxCmd.BindMesh(_ctx.FsqMesh);
        _gfxCmd.DrawMesh();
    }

    private void UseShader(ShaderId shaderId) => _gfxCmd.UseShader(shaderId);
}