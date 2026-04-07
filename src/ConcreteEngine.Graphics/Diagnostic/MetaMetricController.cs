using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Diagnostic;

internal static class MetaMetricController
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool UpdateMax(ref long max, ref int maxIdx, long candidate, int idx)
    {
        if (candidate <= max) return false;
        max = candidate;
        maxIdx = idx;
        return true;
    }

    public static GfxMetaInfo GetTextureMetric(ReadOnlySpan<TextureMeta> metas)
    {
        long max = 0;
        var maxIdx = 0;
        ushort p2 = 0;

        for (var i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
            long dim = m.Width >= m.Height ? m.Width : m.Height;
            if (!UpdateMax(ref max, ref maxIdx, dim, i)) continue;
            var mip = (byte)(m.Levels > 1 ? 1 : 0);
            p2 = (ushort)(mip | (m.Samples << 1));
        }

        return new GfxMetaInfo(max, maxIdx + 1, p2);
    }

    public static GfxMetaInfo GetShaderMetric(ReadOnlySpan<ShaderMeta> metas)
    {
        long max = 0;
        var maxIdx = 0;
        for (var i = 0; i < metas.Length; i++)
        {
            long v = metas[i].SamplerSlots;
            UpdateMax(ref max, ref maxIdx, v, i);
        }

        return new GfxMetaInfo(max, maxIdx + 1, 0);
    }

    public static GfxMetaInfo GetMeshMetric(ReadOnlySpan<MeshMeta> metas)
    {
        long max = 0;
        var maxIdx = 0;
        for (var i = 0; i < metas.Length; i++)
        {
            long v = metas[i].DrawCount;
            UpdateMax(ref max, ref maxIdx, v, i);
        }

        return new GfxMetaInfo(max, maxIdx + 1, 0);
    }


    public static GfxMetaInfo GetVboMetric(ReadOnlySpan<VertexBufferMeta> metas)
    {
        long max = 0;
        int maxIdx = 0, stride = 0;

        for (var i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
            if (UpdateMax(ref max, ref maxIdx, m.Capacity, i))
                stride = m.Stride;
        }

        return new GfxMetaInfo(max, maxIdx + 1, stride);
    }

    public static GfxMetaInfo GetIboMetric(ReadOnlySpan<IndexBufferMeta> metas)
    {
        long max = 0;
        int maxIdx = 0, stride = 0;
        for (var i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
            if (UpdateMax(ref max, ref maxIdx, m.Capacity, i))
                stride = m.Stride;
        }

        return new GfxMetaInfo(max, maxIdx + 1, stride);
    }

    public static GfxMetaInfo GetUboMetric(ReadOnlySpan<UniformBufferMeta> metas)
    {
        long max = 0;
        int maxIdx = 0, stride = 0;
        for (var i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
            if (UpdateMax(ref max, ref maxIdx, m.Capacity, i))
                stride = m.Stride;
        }

        return new GfxMetaInfo(max, maxIdx + 1, stride);
    }


    public static GfxMetaInfo GetFboMetric(ReadOnlySpan<FrameBufferMeta> metas)
    {
        long max = 0;
        int maxIdx = 0, attach = 0;
        for (var i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
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

    public static GfxMetaInfo GetRboMetric(ReadOnlySpan<RenderBufferMeta> metas)
    {
        long max = 0;
        var maxIdx = 0;

        for (var i = 0; i < metas.Length; i++)
        {
            var v = (long)metas[i].Multisample;
            UpdateMax(ref max, ref maxIdx, v, i);
        }

        return new GfxMetaInfo(max, maxIdx + 1, 0);
    }
}