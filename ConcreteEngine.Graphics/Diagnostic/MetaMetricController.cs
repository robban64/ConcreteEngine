using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

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

    public static GfxMetaSpecialMetric GetTextureMetric(ReadOnlySpan<TextureMeta> metas)
    {
        (long max, int maxIdx, ushort p2) = (0, 0, 0);

        for (int i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
            long dim = m.Width >= m.Height ? m.Width : m.Height;
            if (!UpdateMax(ref max, ref maxIdx, dim, i)) continue;
            byte mip = (byte)(m.Levels > 1 ? 1 : 0);
            p2 = (ushort)(mip | (m.Samples << 1));
        }

        return new GfxMetaSpecialMetric(max, maxIdx + 1, p2, ResourceKind.Texture);
    }

    public static GfxMetaSpecialMetric GetShaderMetric(ReadOnlySpan<ShaderMeta> metas)
    {
        (long max, int maxIdx) = (0, 0);
        for (int i = 0; i < metas.Length; i++)
        {
            long v = metas[i].SamplerSlots;
            UpdateMax(ref max, ref maxIdx, v, i);
        }

        return new GfxMetaSpecialMetric(max, maxIdx + 1, 0, ResourceKind.Shader);
    }

    public static GfxMetaSpecialMetric GetMeshMetric(ReadOnlySpan<MeshMeta> metas)
    {
        (long max, int maxIdx) = (0, 0);
        for (int i = 0; i < metas.Length; i++)
        {
            long v = metas[i].DrawCount;
            UpdateMax(ref max, ref maxIdx, v, i);
        }

        return new GfxMetaSpecialMetric(max, maxIdx + 1, 0, ResourceKind.Mesh);
    }


    public static GfxMetaSpecialMetric GetVboMetric(ReadOnlySpan<VertexBufferMeta> metas)
    {
        (long max, int maxIdx, ushort stride) = (0, 0, 0);
        for (int i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
            if (UpdateMax(ref max, ref maxIdx, m.Capacity, i))
                stride = (ushort)m.Stride;
        }

        return new GfxMetaSpecialMetric(max, maxIdx + 1, stride, ResourceKind.VertexBuffer);
    }

    public static GfxMetaSpecialMetric GetIboMetric(ReadOnlySpan<IndexBufferMeta> metas)
    {
        (long max, int maxIdx, ushort stride) = (0, 0, 0);
        for (int i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
            if (UpdateMax(ref max, ref maxIdx, m.Capacity, i))
                stride = (ushort)m.Stride;
        }

        return new GfxMetaSpecialMetric(max, maxIdx + 1, stride, ResourceKind.IndexBuffer);
    }

    public static GfxMetaSpecialMetric GetUboMetric(ReadOnlySpan<UniformBufferMeta> metas)
    {
        (long max, int maxIdx, ushort stride) = (0, 0, 0);
        for (int i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
            if (UpdateMax(ref max, ref maxIdx, m.Capacity, i))
                stride = (ushort)m.Stride;
        }

        return new GfxMetaSpecialMetric(max, maxIdx + 1, stride, ResourceKind.UniformBuffer);
    }


    public static GfxMetaSpecialMetric GetFboMetric(ReadOnlySpan<FrameBufferMeta> metas)
    {
        (long max, int maxIdx, int attach) = (0, 0, 0);

        for (int i = 0; i < metas.Length; i++)
        {
            ref readonly var m = ref metas[i];
            long pix = (long)m.Size.Width * m.Size.Height;
            if (!UpdateMax(ref max, ref maxIdx, pix, i)) continue;

            ref readonly var a = ref m.Attachments;
            int cnt = 0;
            if (a.ColorTextureId.Value > 0) cnt++;
            if (a.DepthTextureId.Value > 0) cnt++;
            if (a.ColorRenderBufferId.Value > 0) cnt++;
            if (a.DepthRenderBufferId.Value > 0) cnt++;
            attach = cnt;
        }

        return new GfxMetaSpecialMetric(max, maxIdx + 1, (ushort)(attach), ResourceKind.FrameBuffer);
    }

    public static GfxMetaSpecialMetric GetRboMetric(ReadOnlySpan<RenderBufferMeta> metas)
    {
        (long max, int maxIdx) = (0, 0);
        for (int i = 0; i < metas.Length; i++)
        {
            long v = (long)metas[i].Multisample;
            UpdateMax(ref max, ref maxIdx, v, i);
        }

        return new GfxMetaSpecialMetric(max, maxIdx + 1, 0, ResourceKind.RenderBuffer);
    }
}