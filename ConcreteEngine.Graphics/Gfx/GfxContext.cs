#region

using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;

#endregion

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxContext
{
    private readonly GfxBuffers _buffers;
    private readonly GfxMeshes _meshes;
    private readonly GfxShaders _shaders;
    private readonly GfxTextures _textures;
    private readonly GfxFrameBuffers _frameBuffers;
    private readonly GfxCommands _cmd;

    private readonly GfxResourceContext _resourceContext;

    public IPrimitiveMeshes Primitives { get; }

    internal GfxContext(GfxContextInternal ctxInternal, GfxResourceContext resourceContext)
    {
        _buffers = new GfxBuffers(ctxInternal);
        _shaders = new GfxShaders(ctxInternal);
        _textures = new GfxTextures(ctxInternal);
        _meshes = new GfxMeshes(ctxInternal, _buffers);
        _frameBuffers = new GfxFrameBuffers(ctxInternal, _textures);
        _cmd = new GfxCommands(ctxInternal);

        _resourceContext = resourceContext;

        var primitiveMeshes = new PrimitiveMeshes();
        primitiveMeshes.CreatePrimitives(_meshes);
        Primitives = primitiveMeshes;
    }

    public GfxBuffers Buffers => _buffers;

    public GfxMeshes Meshes => _meshes;

    public GfxShaders Shaders => _shaders;

    public GfxTextures Textures => _textures;

    public GfxFrameBuffers FrameBuffers => _frameBuffers;

    public GfxCommands Commands => _cmd;

    public IGfxResourceContext ResourceContext => _resourceContext;
}