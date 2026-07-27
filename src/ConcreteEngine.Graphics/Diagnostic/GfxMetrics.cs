using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Graphics.Diagnostic;

public static class GfxMetrics
{
    public static GpuFrameMeta FrameMeta;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void AddDrawCall(uint tris, uint instances)
    {
        ref var it = ref FrameMeta.Frame;
        it.Draws++;
        it.Tris += tris;
        it.Instances += instances;
    }


    public static void DrainStoreMetrics(GfxStoreMeta[] data)
    {
        var i = 0;
        foreach (var store in GfxRegistry.GetStores())
        {
            store.FillGfxStoreMeta(out data[i++]);
        }
    }

    internal static GfxMetaInfo GetSpecialMetric(GraphicsKind kind)
    {
        return kind switch
        {
            GraphicsKind.Texture => GetTextureMetric(),
            GraphicsKind.Shader => GetShaderMetric(),
            GraphicsKind.Mesh => GetMeshMetric(),
            GraphicsKind.VertexBuffer => GetVboMetric(),
            GraphicsKind.IndexBuffer => GetIboMetric(),
            GraphicsKind.UniformBuffer => GetUboMetric(),
            GraphicsKind.FrameBuffer => GetFboMetric(),
            GraphicsKind.RenderBuffer => GetRboMetric(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static GfxMetaInfo GetTextureMetric()
    {
        long max = 0;
        var maxIdx = 0;
        ushort p2 = 0;

        var count = GfxRegistry.TextureStore.Count;
        for (var i = 0; i < count; i++)
        {
            if (!GfxRegistry.TextureStore.TryGet(new(i + 1), out var m).IsValid())
                continue;
            long dim = m.Width >= m.Height ? m.Width : m.Height;
            if (!UpdateMax(ref max, ref maxIdx, dim, i)) continue;
            var mip = (byte)(m.MipLevels > 1 ? 1 : 0);
            p2 = (ushort)(mip | (m.Samples << 1));
        }

        return new GfxMetaInfo(max, maxIdx + 1, p2);
    }

    private static GfxMetaInfo GetShaderMetric()
    {
        long max = 0;
        var maxIdx = 0;

        var count = GfxRegistry.ShaderStore.Count;
        for (var i = 0; i < count; i++)
        {
            if (!GfxRegistry.ShaderStore.TryGet(new(i + 1), out var m).IsValid())
                continue;
            long v = m.SamplerSlots;
            UpdateMax(ref max, ref maxIdx, v, i);
        }

        return new GfxMetaInfo(max, maxIdx + 1, 0);
    }

    private static GfxMetaInfo GetMeshMetric()
    {
        long max = 0;
        var maxIdx = 0;
        var count = GfxRegistry.MeshStore.Count;
        for (var i = 0; i < count; i++)
        {
            if (!GfxRegistry.MeshStore.TryGet(new(i + 1), out var m).IsValid())
                continue;
            UpdateMax(ref max, ref maxIdx, m.DrawCount, i);
        }

        return new GfxMetaInfo(max, maxIdx + 1, 0);
    }


    private static GfxMetaInfo GetVboMetric()
    {
        long max = 0;
        int maxIdx = 0, stride = 0;

        var count = GfxRegistry.VboStore.Count;
        for (var i = 0; i < count; i++)
        {
            if (!GfxRegistry.VboStore.TryGet(new(i + 1), out var m).IsValid())
                continue;

            if (UpdateMax(ref max, ref maxIdx, m.Capacity, i))
                stride = m.Stride;
        }

        return new GfxMetaInfo(max, maxIdx + 1, stride);
    }

    private static GfxMetaInfo GetIboMetric()
    {
        long max = 0;
        int maxIdx = 0, stride = 0;
        var count = GfxRegistry.IboStore.Count;
        for (var i = 0; i < count; i++)
        {
            if (!GfxRegistry.IboStore.TryGet(new(i + 1), out var m).IsValid())
                continue;
            if (UpdateMax(ref max, ref maxIdx, m.Capacity, i))
                stride = m.Stride;
        }

        return new GfxMetaInfo(max, maxIdx + 1, stride);
    }

    private static GfxMetaInfo GetUboMetric()
    {
        long max = 0;
        int maxIdx = 0, stride = 0;
        var count = GfxRegistry.UboStore.Count;
        for (var i = 0; i < count; i++)
        {
            if (!GfxRegistry.UboStore.TryGet(new(i + 1), out var m).IsValid())
                continue;
            if (UpdateMax(ref max, ref maxIdx, m.Capacity, i))
                stride = m.Stride;
        }

        return new GfxMetaInfo(max, maxIdx + 1, stride);
    }


    private static GfxMetaInfo GetFboMetric()
    {
        long max = 0;
        int maxIdx = 0, attach = 0;
        var count = GfxRegistry.FboStore.Count;
        for (var i = 0; i < count; i++)
        {
            if (!GfxRegistry.FboStore.TryGet(new(i + 1), out var m).IsValid())
                continue;
            var pix = (long)m.Size.Width * m.Size.Height;
            if (!UpdateMax(ref max, ref maxIdx, pix, i)) continue;

            ref readonly var a = ref m.Attachments;
            var cnt = 0;
            if (a.ColorTexture > 0) cnt++;
            if (a.DepthTexture > 0) cnt++;
            if (a.ColorRbo > 0) cnt++;
            if (a.DepthRbo > 0) cnt++;
            attach = cnt;
        }

        return new GfxMetaInfo(max, maxIdx + 1, attach);
    }

    private static GfxMetaInfo GetRboMetric()
    {
        long max = 0;
        var maxIdx = 0;

        var count = GfxRegistry.RboStore.Count;
        for (var i = 0; i < count; i++)
        {
            if (!GfxRegistry.RboStore.TryGet(new(i + 1), out var m).IsValid())
                continue;
            var v = (long)m.Multisample;
            UpdateMax(ref max, ref maxIdx, v, i);
        }

        return new GfxMetaInfo(max, maxIdx + 1, 0);
    }

    private static bool UpdateMax(ref long max, ref int maxIdx, long candidate, int idx)
    {
        if (candidate <= max) return false;
        max = candidate;
        maxIdx = idx;
        return true;
    }
}