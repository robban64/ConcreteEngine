#region

using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Gfx;

public sealed class RenderCommandOps
{
    private readonly GfxCommands _gfxCmd;
    private readonly GfxTextures _gfxTextures;
    private readonly DrawProcessor _drawProcessor;

    internal RenderCommandOps(GfxContext ctx, DrawProcessor drawProcessor)
    {
        _drawProcessor = drawProcessor;
        _gfxCmd = ctx.Commands;
        _gfxTextures = ctx.Textures;
    }

    public void BeginScreenPass(in GfxPassClear passClear, in GfxPassState states) =>
        _gfxCmd.BeginScreenPass(in passClear, in states);

    public void BeginRenderPass(FrameBufferId fboId, in GfxPassClear passClear, in GfxPassState states) =>
        _gfxCmd.BeginRenderPass(fboId, in passClear, in states);

    public void DrawFullscreenQuad(ShaderId shaderId, ReadOnlySpan<TextureId> sources) =>
        _drawProcessor.DrawFullscreenQuad(shaderId, sources);

    public void DrawFullscreenQuad(ShaderId shaderId, IReadOnlyList<TextureId> sources) =>
        _drawProcessor.DrawFullscreenQuad(shaderId, sources);

    public void Blit(FrameBufferId from, FrameBufferId target, bool linear) =>
        _gfxCmd.BlitFramebuffer(from, target, linear);

    public void ClearColor(in GfxPassClear clear) => _gfxCmd.Clear(clear);

    public void ToggleStates(in GfxPassState states) => _gfxCmd.ApplyState(states);

    public void GenerateMips(TextureId textureId) => _gfxTextures.GenerateMipMaps(textureId);
}