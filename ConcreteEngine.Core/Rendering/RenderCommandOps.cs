using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginScreenPass( in GfxPassClear passClear)
        => _gfxCmd.BeginScreenPass( in passClear);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginRenderPass(FrameBufferId fboId, in GfxPassClear passClear, bool scenePass)
         => _gfxCmd.BeginRenderPass(fboId, in passClear, scenePass);
    

    public void Blit(FrameBufferId from, FrameBufferId target, bool linear) => _gfxCmd.BlitFramebuffer(from, target, linear);

    
    public void ClearColor(in GfxPassClear clear) => _gfxCmd.Clear(clear);

    public void UpdateStates(GfxPassState states) => _gfxCmd.ApplyState(states);

    public void GenerateMips(TextureId textureId) => _gfxTextures.GenerateMipMaps(textureId);
}