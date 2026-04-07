using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Configuration;

internal static class Warmup
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static long WarmupStore(BackendStoreHub bk, GfxStoreHub gfx)
    {
        long result = 0;
        result += bk.TextureStore.GetHandle(new GfxHandle(1, 1, GraphicsKind.Texture));
        result += bk.ShaderStore.GetHandle(new GfxHandle(1, 1, GraphicsKind.Shader));
        result += bk.MeshStore.GetHandle(new GfxHandle(1, 1,GraphicsKind.Mesh));
        result += bk.VboStore.GetHandle(new GfxHandle(1, 1,GraphicsKind.VertexBuffer));
        result += bk.IboStore.GetHandle(new GfxHandle(1, 1,GraphicsKind.IndexBuffer));
        result += bk.FboStore.GetHandle(new GfxHandle(1, 1,GraphicsKind.FrameBuffer));
        result += bk.RboStore.GetHandle(new GfxHandle(1, 1,GraphicsKind.RenderBuffer));
        result += bk.UboStore.GetHandle(new GfxHandle(1, 1,GraphicsKind.UniformBuffer));

        result += gfx.TextureStore.TryGet(new TextureId(0), out _).Slot;
        result += gfx.ShaderStore.TryGet(new ShaderId(1), out _).Slot;
        result += gfx.MeshStore.TryGet(new MeshId(0), out _).Slot;
        result += gfx.VboStore.TryGet(new VertexBufferId(1), out _).Slot;
        result += gfx.IboStore.TryGet(new IndexBufferId(0), out _).Slot;
        result += gfx.FboStore.TryGet(new FrameBufferId(1), out _).Slot;
        result += gfx.RboStore.TryGet(new RenderBufferId(0), out _).Slot;
        result += gfx.UboStore.TryGet(new UniformBufferId(1), out _).Slot;

        return result;
    }
}