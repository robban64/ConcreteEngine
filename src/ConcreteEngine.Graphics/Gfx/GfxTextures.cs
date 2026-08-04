using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.OpenGL;
using static ConcreteEngine.Graphics.Gfx.Internals.GfxTextureUtils;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxTextures
{
    public static class Fallback
    {
        public static TextureId AlbedoId { get; internal set; }
        public static TextureId NormalId { get; internal set; }
        public static TextureId AlphaMaskId { get; internal set; }
    }

    private static readonly NativeHandle[] Samplers = new NativeHandle[9];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static NativeHandle GetSamplerHandle(SamplerProfile profile) => Samplers[(int)profile];

    private readonly GfxResourceDisposer _disposer;

    internal GfxTextures(GfxResourceDisposer disposer)
    {
        _disposer = disposer;

        CreateSampler(SamplerProfile.PointClamp, TexturePreset.NearestClamp);
        CreateSampler(SamplerProfile.PointWrap, TexturePreset.NearestRepeat);

        CreateSampler(SamplerProfile.LinearClamp, TexturePreset.LinearClamp);
        CreateSampler(SamplerProfile.LinearWrap, TexturePreset.LinearRepeat);

        CreateSampler(SamplerProfile.TrilinearClamp, TexturePreset.LinearMipmapClamp);
        CreateSampler(SamplerProfile.TrilinearWrap, TexturePreset.LinearMipmapRepeat);

        CreateSampler(SamplerProfile.AnisotropicClamp, TexturePreset.LinearMipmapClamp, TextureAnisotropy.X8, lod: -0.5f);
        CreateSampler(SamplerProfile.AnisotropicWrap, TexturePreset.LinearMipmapRepeat, TextureAnisotropy.X8, lod: -0.5f);
        CreateSampler(SamplerProfile.ShadowCompare, TexturePreset.NearestClampBorder, depthMode: DepthMode.Lequal);
        GlStates.BindSampler(default, 0);


        Fallback.AlbedoId = CreateOnePixelTexture([255, 255, 255, 255], TexturePixelFormat.SrgbAlpha);
        Fallback.NormalId = CreateOnePixelTexture([128, 128, 255], TexturePixelFormat.Rgb);
        Fallback.AlphaMaskId = CreateOnePixelTexture([255], TexturePixelFormat.Red, TexturePreset.NearestClamp);
    }

    private void CreateSampler(
        SamplerProfile profile,
        TexturePreset preset,
        TextureAnisotropy anisotropy = TextureAnisotropy.Off,
        TextureKind kind = TextureKind.Texture2D,
        TextureBorder border = default,
        DepthMode depthMode = DepthMode.Unset,
        float lod = 0)
    {
        if(preset == TexturePreset.None) Throwers.InvalidArgument(nameof(preset));
        
        var sampler = GlTextures.CreateSampler();
        GlTextures.SetSamplerPreset(sampler, preset, SupportsWrapR(kind));

        if (border.Enabled)
            GlTextures.SetSamplerBorder(sampler, border);

        if (anisotropy != TextureAnisotropy.Off)
            GlTextures.SetSamplerAnisotropy(sampler, anisotropy.ToAnisotropy());

        if (lod != 0)
            GlTextures.SetSamplerLodBias(sampler, lod);

        if (depthMode != DepthMode.Unset)
            GlTextures.SetSamplerCompareTextureFunc(sampler, depthMode);

        Samplers[(int)profile] = sampler;
    }

    private TextureId CreateOnePixelTexture(Span<byte> pixelData, TexturePixelFormat format,
        TexturePreset preset = TexturePreset.NearestRepeat)
    {
        //TexturePreset.NearestRepeat
        var texture = CreateTexture2D(pixelData, Size2D.One, format);
        var handle = GfxRegistry.TextureStore.GetHandle(texture);
        GlTextures.SetTexturePreset(handle,preset, false);
        return texture;
    }

    private TextureId CreateTexture(Size3D size, TextureKind kind, TexturePixelFormat format,
        RenderBufferMsaa samples = RenderBufferMsaa.None, TextureBorder border = default)
    {
        var handle = CreateDriverTexture(size, kind, format, border, samples, out var meta);
        var textureId = GfxRegistry.TextureStore.Add(in meta, handle);
        return textureId;
    }

    //
    public TextureId CreateTexture2D(ReadOnlySpan<byte> data, Size2D size, TexturePixelFormat format)
    {
        var textureId = CreateTexture(size.ToSize3D(1), TextureKind.Texture2D, format);
        UploadTexture2D(textureId, data, size);
        ApplyProperties(textureId);
        return textureId;
    }

    public TextureId CreateTextureEmpty(Size3D size, TextureKind kind, TexturePixelFormat format,
        RenderBufferMsaa samples = RenderBufferMsaa.None, TextureBorder border = default)
    {
        var textureId = CreateTexture(size, kind, format, samples, border);
        ApplyProperties(textureId);
        return textureId;
    }

    public TextureId CreateCubeMap(Size2D size, TexturePixelFormat format)
    {
        var textureId = CreateTexture(size.ToSize3D(1), TextureKind.CubeMap, format);
        ApplyProperties(textureId);
        return textureId;
    }

    public TextureId CreateTexture2DArrayFrom(TextureId baseTexId, int layerCount)
    {
        ArgumentOutOfRangeException.ThrowIfZero(baseTexId.Id, nameof(baseTexId));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(layerCount, 1);

        var baseMeta = GfxRegistry.TextureStore.GetMeta(baseTexId);
        if (baseMeta.Kind != TextureKind.Texture2D) throw new GraphicsException(nameof(baseMeta.Kind));

        var handle = GlTextures.CreateTexture(TextureKind.Texture2DArray);

        var gpuProps = new GpuTextureProps(baseMeta.PixelFormat, baseMeta.MipLevels, baseMeta.Samples);
        GlTextures.TextureStorage3D(handle, new Size3D(baseMeta.Width, baseMeta.Height, layerCount), gpuProps);

        var meta = baseMeta with { Kind = TextureKind.Texture2DArray, Depth = (ushort)layerCount };
        var textureId = GfxRegistry.TextureStore.Add(in meta, handle);

        ApplyProperties(textureId);
        return textureId;
    }

    public void SetTexture2DArrayLayerFrom(TextureId arrayId, TextureId srcId, int layer)
    {
        ArgumentOutOfRangeException.ThrowIfZero(arrayId.Id, nameof(arrayId));
        ArgumentOutOfRangeException.ThrowIfZero(srcId.Id, nameof(srcId));
        ArgumentOutOfRangeException.ThrowIfNegative(layer);

        var dstHandle = GfxRegistry.TextureStore.GetHandleAndMeta(arrayId, out var dstMeta);
        var srcHandle = GfxRegistry.TextureStore.GetHandleAndMeta(srcId, out var srcMeta);

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(layer, dstMeta.GetArrayLength());

        ValidateTexture2DArrayMeta(in dstMeta, in srcMeta);

        var size = dstMeta.AsSize2D();
        for (int mip = 0; mip < dstMeta.MipLevels; mip++)
        {
            GlTextures.CopyTextureData(
                src: srcHandle,
                srcKind: TextureKind.Texture2D,
                dst: dstHandle,
                dstKind: TextureKind.Texture2DArray,
                srcLevel: mip,
                dstLevel: mip,
                srcSize: CalcMipSize(mip, size).ToSize3D(1),
                dstPos: new Int3(0, 0, layer)
            );
        }
    }

    internal NativeHandle ReplaceTexture(TextureId textureId, Size3D size, int? samples = null)
    {
        var handle = GfxRegistry.TextureStore.GetHandleAndMeta(textureId, out var meta);
        _disposer.EnqueueReplace(textureId, handle);

        samples = meta.Kind == TextureKind.Multisample2D ? samples ?? meta.Samples : samples;
        var msaa = GfxEnumUtils.ToRenderBufferMsaa(samples);

        ValidateRecreateTexture(size, samples, in meta);

        var newHandle = CreateDriverTexture(size, meta.Kind, meta.PixelFormat, meta.BorderColor, msaa, out var newMeta);
        GfxRegistry.TextureStore.Replace(textureId, in newMeta, newHandle);
        return newHandle;
    }

    public void ApplyProperties(TextureId textureId)
    {
        var handle = GfxRegistry.TextureStore.GetHandleAndMeta(textureId, out var meta);
        if (meta.IsMsaa) return;

        if (meta.BorderColor.Enabled)
            GlTextures.SetBorder(handle, meta.BorderColor);

        if (meta.MipLevels > 1 && meta.Kind != TextureKind.Texture2DArray)
            GlTextures.GenerateMipMaps(handle);

    }


    public void UploadTexture2D(TextureId textureId, ReadOnlySpan<byte> data, Size2D size)
    {
        var handle = GfxRegistry.TextureStore.GetHandleAndMeta(textureId, out var meta);
        if (meta.Kind == TextureKind.Unknown) throw new GraphicsException(nameof(meta.Kind));

        ValidateUploadSize(size, meta.AsSize2D());

        GlTextures.UploadTexture2D_Data(handle, data, meta.PixelFormat, size);
    }

    public void UploadTexture3D(TextureId textureId, ReadOnlySpan<byte> data, int width, int height, int depth)
    {
        var handle = GfxRegistry.TextureStore.GetHandleAndMeta(textureId, out var meta);
        if (meta.Kind != TextureKind.Texture3D) throw new GraphicsException(nameof(meta.Kind));

        var (size, metaSize) = (new Size3D(width, height, depth), new Size3D(meta.Width, meta.Height, meta.Depth));
        ValidateUploadSize3D(size, metaSize);

        GlTextures.UploadTexture3D_Data(handle, data, meta.PixelFormat, size, zOffset: 0);
    }

    public void UploadCubeMapFace(TextureId textureId, ReadOnlySpan<byte> data, Size2D size, int faceIndex)
    {
        ArgumentOutOfRangeException.ThrowIfZero(textureId.Id, nameof(textureId.Id));
        ArgumentOutOfRangeException.ThrowIfZero(data.Length, nameof(data.Length));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(faceIndex, 5);

        var handle = GfxRegistry.TextureStore.GetHandleAndMeta(textureId, out var meta);
        if (meta.Kind != TextureKind.CubeMap) throw new GraphicsException(nameof(meta.Kind));

        ValidateUploadSize(size, meta.AsSize2D());

        GlTextures.UploadTexture3D_Data(handle, data, meta.PixelFormat, size.ToSize3D(1), faceIndex);
    }
    
    private NativeHandle CreateDriverTexture(Size3D size, TextureKind kind, TexturePixelFormat format,
        TextureBorder border, RenderBufferMsaa samples, out TextureMeta meta)
    {
        var levels = CalcMipLevels(size.Width, size.Height, size.Depth);
        if (levels < 1) throw new GraphicsException(nameof(levels));
        ValidateTextureDescriptor(size, kind, format, samples, levels);

        var handle = GlTextures.CreateTexture(kind);

        switch (kind)
        {
            case TextureKind.Texture2D:
                GlTextures.TextureStorage2D(handle, size, GpuTextureProps.Make(format, levels, 0));
                break;
            case TextureKind.CubeMap:
                GlTextures.TextureStorage2D(handle, size, GpuTextureProps.Make(format, levels, 0));
                break;
            case TextureKind.Multisample2D:
                var msaaStoreProps = GpuTextureProps.Make(format, levels, samples.ToSamples());
                GlTextures.TextureStorage2D_MultiSample(handle, size, msaaStoreProps);
                break;
            case TextureKind.Texture3D:
                GlTextures.TextureStorage3D(handle, size, GpuTextureProps.Make(format, levels, 0));
                break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }

        meta = new TextureMeta(size.Width, size.Height, (ushort)size.Depth, (byte)levels, (byte)samples.ToSamples(),
            kind, format, border);

        return handle;
    }
}