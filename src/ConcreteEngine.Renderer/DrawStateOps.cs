using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Renderer;

public sealed class DrawStateOps
{
    private static VisualRenderContext RenderContext => VisualRenderContext.Instance;

    private readonly GfxCommands _gfxCmd;
    private readonly GfxTextures _gfxTextures;
    private readonly GfxDraw _gfxDraw;

    private readonly UniformUploader _uniformUploader;
    private readonly DrawStateContext _ctx;

    internal DrawStateOps(DrawStateContext ctx, DrawStateContextPayload ctxPayload, UniformUploader uniformUploader)
    {
        _uniformUploader = uniformUploader;
        _gfxCmd = ctxPayload.Gfx.Commands;
        _gfxTextures = ctxPayload.Gfx.Textures;
        _gfxDraw = ctxPayload.Gfx.Draw;

        _ctx = ctx;
    }


}