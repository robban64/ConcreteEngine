using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Configuration;

internal static class Warmup
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static long WarmupStore(BackendStoreHub bk, GfxStoreHub gfx)
    {
        long result = 0;
        for (int i = 0; i < 30; i++)
        {
            result += bk.TextureStore.GetHandle(new GfxRefToken<TextureId>(1, 1));
            result += bk.ShaderStore.GetHandle(new GfxRefToken<ShaderId>(1, 1));
            result += bk.MeshStore.GetHandle(new GfxRefToken<MeshId>(1, 1));
            result += bk.VboStore.GetHandle(new GfxRefToken<VertexBufferId>(1, 1));
            result += bk.IboStore.GetHandle(new GfxRefToken<IndexBufferId>(1, 1));
            result += bk.FboStore.GetHandle(new GfxRefToken<FrameBufferId>(1, 1));
            result += bk.RboStore.GetHandle(new GfxRefToken<RenderBufferId>(1, 1));
            result += bk.UboStore.GetHandle(new GfxRefToken<UniformBufferId>(1, 1));

            result += gfx.TextureStore.TryGet(new TextureId(1), out _).Slot;
            result += gfx.ShaderStore.TryGet(new ShaderId(1), out _).Slot;
            result += gfx.MeshStore.TryGet(new MeshId(1), out _).Slot;
            result += gfx.VboStore.TryGet(new VertexBufferId(1), out _).Slot;
            result += gfx.IboStore.TryGet(new IndexBufferId(1), out _).Slot;
            result += gfx.FboStore.TryGet(new FrameBufferId(1), out _).Slot;
            result += gfx.RboStore.TryGet(new RenderBufferId(1), out _).Slot;
            result += gfx.UboStore.TryGet(new UniformBufferId(1), out _).Slot;
        }

        return result;
    }
}