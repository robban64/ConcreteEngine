using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics;

public sealed class GfxContext
{
    public required IGfxResourceManager ResourceManager { get; init; }
    public required IGfxResourceDisposer Disposer { get; init; }

    public required GfxCommands Commands { get; init; }
    public required GfxDraw Draw { get; init; }
    public required GfxBuffers Buffers { get; init; }
    public required GfxMeshes Meshes { get; init; }
    public required GfxShaders Shaders { get; init; }
    public required GfxTextures Textures { get; init; }
    public required GfxFrameBuffers FrameBuffers { get; init; }
}