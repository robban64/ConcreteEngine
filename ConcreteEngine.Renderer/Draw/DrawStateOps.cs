#region

using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Utils;

#endregion

namespace ConcreteEngine.Core.Rendering.Draw;

public sealed class DrawStateOps
{
    private readonly IPrimitiveMeshes _primitiveMeshes;
    private readonly GfxCommands _gfxCmd;
    private readonly GfxTextures _gfxTextures;
    private readonly RenderRegistry _renderRegistry;
    private readonly RenderView _renderView;
    private readonly RenderSceneSnapshot _sceneSnapshot;
    private readonly DrawBuffers _drawBuffers;

    private readonly DrawStateContext _ctx;

    internal DrawStateOps(DrawStateContext ctx, DrawStateContextPayload ctxPayload, DrawBuffers drawBuffers)
    {
        _renderRegistry = ctxPayload.Registry;
        _renderView = ctxPayload.RenderView;
        _sceneSnapshot = ctxPayload.Snapshot;
        _drawBuffers = drawBuffers;
        _gfxCmd = ctxPayload.Gfx.Commands;
        _gfxTextures = ctxPayload.Gfx.Textures;
        _primitiveMeshes = ctxPayload.Gfx.Primitives;

        _ctx = ctx;
    }

    public void ActivateDepthMode()
    {
        _ctx.SetDepthMode();

        _renderView.ApplyLightViewOverride(_sceneSnapshot.DirLight.Direction);
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
        => _gfxCmd.ApplyStateFunctions(passFunc);

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
        //var renderShader = _renderRegistry.GetRenderShader(shaderId);
        _gfxCmd.UseShader(shaderId);
    }
}