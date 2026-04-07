using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Renderer.Draw;

public sealed class DrawStateOps
{
    private readonly GfxCommands _gfxCmd;
    private readonly GfxTextures _gfxTextures;
    private readonly DrawBuffers _drawBuffers;

    private readonly DrawStateContext _ctx;

    private readonly VisualRenderContext _visualContext = VisualRenderContext.Instance;
    
    internal DrawStateOps(DrawStateContext ctx, DrawStateContextPayload ctxPayload, DrawBuffers drawBuffers)
    {
        _drawBuffers = drawBuffers;
        _gfxCmd = ctxPayload.Gfx.Commands;
        _gfxTextures = ctxPayload.Gfx.Textures;

        _ctx = ctx;
    }

    public void ActivateDepthMode()
    {
        _ctx.SetDepthMode();

        _visualContext.Camera.UseLightSpace = true;
        _drawBuffers.UploadCameraView();
        _drawBuffers.UploadShadow();
    }

    public void RestoreMode()
    {
        _ctx.ResetPassMode();

        _visualContext.Camera.UseLightSpace = false;
        _drawBuffers.UploadCameraView();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyStateFunctions(GfxPassFunctions passFunc)
    {
        _gfxCmd.ApplyStateFunctions(passFunc);
        _ctx.PassFunctions = passFunc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginScreenPass(GfxPassClear passClear, GfxPassState states)
    {
        _gfxCmd.BeginScreenPass(passClear, states);
        _ctx.PassState = states;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginRenderPass(FrameBufferId fboId, GfxPassClear passClear, GfxPassState states)
    {
        _gfxCmd.BeginRenderPass(fboId, passClear, states);
        _ctx.PassState = states;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ContinueFromRenderPass(FrameBufferId fboId, GfxPassState states)
    {
        _gfxCmd.BindFramebuffer(fboId);
        _gfxCmd.ApplyState(states);
        _ctx.PassState = states;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndRenderPass() => _gfxCmd.EndRenderPass();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Blit(FrameBufferId from, FrameBufferId target, bool linear) =>
        _gfxCmd.BlitFramebuffer(from, target, linear);

    public void ClearColor(GfxPassClear clear) => _gfxCmd.Clear(clear);

    public void ToggleStates(GfxPassState states) => _gfxCmd.ApplyState(states);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        _gfxCmd.BindMesh(GfxMeshes.FsqQuad);
        _gfxCmd.DrawMesh();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UseShader(ShaderId shaderId) => _gfxCmd.UseShader(shaderId);
}