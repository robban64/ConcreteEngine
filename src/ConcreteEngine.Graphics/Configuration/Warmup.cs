using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Configuration;

internal static class Warmup
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static long WarmupStore(BackendStoreBundle storeBundle, GfxStoreHub gfx)
    {
        long result = 0;
        for (int i = 0; i < 30; i++)
        {
            result += storeBundle.Texture.GetHandle(new GfxRefToken<TextureId>(1, 1));
            result += storeBundle.Shader.GetHandle(new GfxRefToken<ShaderId>(1, 1));
            result += storeBundle.VertexArray.GetHandle(new GfxRefToken<MeshId>(1, 1));
            result += storeBundle.VertexBuffer.GetHandle(new GfxRefToken<VertexBufferId>(1, 1));
            result += storeBundle.IndexBuffer.GetHandle(new GfxRefToken<IndexBufferId>(1, 1));
            result += storeBundle.FrameBuffer.GetHandle(new GfxRefToken<FrameBufferId>(1, 1));
            result += storeBundle.RenderBuffer.GetHandle(new GfxRefToken<RenderBufferId>(1, 1));
            result += storeBundle.UniformBuffer.GetHandle(new GfxRefToken<UniformBufferId>(1, 1));

            result += gfx.TextureStore.TryGetRef(new TextureId(1), out _).Slot;
            result += gfx.ShaderStore.TryGetRef(new ShaderId(1), out _).Slot;
            result += gfx.MeshStore.TryGetRef(new MeshId(1), out _).Slot;
            result += gfx.VboStore.TryGetRef(new VertexBufferId(1), out _).Slot;
            result += gfx.IboStore.TryGetRef(new IndexBufferId(1), out _).Slot;
            result += gfx.FboStore.TryGetRef(new FrameBufferId(1), out _).Slot;
            result += gfx.RboStore.TryGetRef(new RenderBufferId(1), out _).Slot;
            result += gfx.UboStore.TryGetRef(new UniformBufferId(1), out _).Slot;
        }

        return result;
    }
}