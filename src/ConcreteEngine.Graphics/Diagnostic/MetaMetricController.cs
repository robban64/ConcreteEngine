using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics;
using ConcreteEngine.Graphics.Gfx.Definitions;
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
        (long max, var maxIdx, ushort p2) = (0, 0, 0);

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
        (long max, var maxIdx) = (0, 0);
        for (var i = 0; i < metas.Length; i++)
        {
            long v = metas[i].SamplerSlots;
            UpdateMax(ref max, ref maxIdx, v, i);
        }

        return new GfxMetaInfo(max, maxIdx + 1, 0);
    }

    public static GfxMetaInfo GetMeshMetric(ReadOnlySpan<MeshMeta> metas)
    {
        (long max, var maxIdx) = (0, 0);
        for (var i = 0; i < metas.Length; i++)
        {
            long v = metas[i].DrawCount;
            UpdateMax(ref max, ref maxIdx, v, i);
        }

        return new GfxMetaInfo(max, maxIdx + 1, 0);
    }


    public static GfxMetaInfo GetVboMetric(ReadOnlySpan<VertexBufferMeta> metas)
    {
        (long max, var maxIdx, ushort stride) = (0, 0, 0);
        for (var i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
            if (UpdateMax(ref max, ref maxIdx, m.Capacity, i))
                stride = (ushort)m.Stride;
        }

        return new GfxMetaInfo(max, maxIdx + 1, stride);
    }

    public static GfxMetaInfo GetIboMetric(ReadOnlySpan<IndexBufferMeta> metas)
    {
        (long max, var maxIdx, ushort stride) = (0, 0, 0);
        for (var i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
            if (UpdateMax(ref max, ref maxIdx, m.Capacity, i))
                stride = (ushort)m.Stride;
        }

        return new GfxMetaInfo(max, maxIdx + 1, stride);
    }

    public static GfxMetaInfo GetUboMetric(ReadOnlySpan<UniformBufferMeta> metas)
    {
        (long max, var maxIdx, ushort stride) = (0, 0, 0);
        for (var i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
            if (UpdateMax(ref max, ref maxIdx, m.Capacity, i))
                stride = (ushort)m.Stride;
        }

        return new GfxMetaInfo(max, maxIdx + 1, stride);
    }


    public static GfxMetaInfo GetFboMetric(ReadOnlySpan<FrameBufferMeta> metas)
    {
        (long max, var maxIdx, var attach) = (0, 0, 0);

        for (var i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
            var pix = (long)m.Size.Width * m.Size.Height;
            if (!UpdateMax(ref max, ref maxIdx, pix, i)) continue;

            ref readonly var a = ref m.Attachments;
            var cnt = 0;
            if (a.ColorTextureId > 0) cnt++;
            if (a.DepthTextureId > 0) cnt++;
            if (a.ColorRenderBufferId > 0) cnt++;
            if (a.DepthRenderBufferId > 0) cnt++;
            attach = cnt;
        }

        return new GfxMetaInfo(max, maxIdx + 1, (ushort)attach);
    }

    public static GfxMetaInfo GetRboMetric(ReadOnlySpan<RenderBufferMeta> metas)
    {
        (long max, var maxIdx) = (0, 0);
        for (var i = 0; i < metas.Length; i++)
        {
            var v = (long)metas[i].Multisample;
            UpdateMax(ref max, ref maxIdx, v, i);
        }

        return new GfxMetaInfo(max, maxIdx + 1, 0);
    }
}