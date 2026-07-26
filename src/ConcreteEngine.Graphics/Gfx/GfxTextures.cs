using System.Diagnostics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
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

    private readonly GfxResourceDisposer _disposer;

    internal GfxTextures(GfxContextInternal context)
    {
        _disposer = context.Disposer;

        Fallback.AlbedoId = CreateOnePixelTexture([255, 255, 255, 255], TexturePixelFormat.SrgbAlpha);
        Fallback.NormalId = CreateOnePixelTexture([128, 128, 255], TexturePixelFormat.Rgb);
        Fallback.AlphaMaskId = CreateOnePixelTexture([255], TexturePixelFormat.Red, TexturePreset.NearestClamp);
    }

    private TextureId CreateOnePixelTexture(byte[] pixelData, TexturePixelFormat format,
        TexturePreset preset = TexturePreset.NearestRepeat)
    {
        var props = new CreateTextureProps(0, TextureKind.Texture2D, format, preset, TextureAnisotropy.Off);
        return CreateTexture2D(new Size2D(1, 1), props, pixelData);
    }

    private TextureId CreateTexture(Size3D size, in CreateTextureProps props)
    {
        var handle = CreateDriverTexture(size, in props, out var meta);
        var textureId = GfxRegistry.TextureStore.Add(in meta, handle);
        return textureId;
    }

    //
    public TextureId CreateTexture2D(Size2D size, in CreateTextureProps props,
        ReadOnlySpan<byte> data)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(props.Kind, TextureKind.Texture2D);
        var textureId = CreateTexture(size.ToSize3D(1), in props);
        UploadTexture2D(textureId, data, size);
        ApplyProperties(textureId);
        return textureId;
    }

    public TextureId CreateTextureEmpty(Size3D size, in CreateTextureProps props)
    {
        var textureId = CreateTexture(size, in props);
        ApplyProperties(textureId);
        return textureId;
    }

    public TextureId CreateCubeMap(Size2D size, in CreateTextureProps props)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(props.Kind, TextureKind.CubeMap, nameof(props.Kind));
        var textureId = CreateTexture(size.ToSize3D(1), in props);
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
        var texHandle = GfxRegistry.TextureStore.GetHandleAndMeta(textureId, out var meta);
        _disposer.EnqueueReplace(textureId, texHandle);

        samples = meta.Kind == TextureKind.Multisample2D ? samples ?? meta.Samples : samples;
        var msaa = GfxEnumUtils.ToRenderBufferMsaa(samples);

        ValidateRecreateTexture(size, samples, in meta);

        var props = new CreateTextureProps((float)meta.Lod, meta.Kind, meta.PixelFormat, meta.Preset, meta.Anisotropy,
            meta.CompareTextureFunc,
            meta.BorderColor, msaa);

        var newHandle = CreateDriverTexture(size, in props, out var newMeta);
        GfxRegistry.TextureStore.Replace(textureId, in newMeta, newHandle);
        return newHandle;
    }

    public void ApplyProperties(TextureId textureId)
    {
        var texHandle = GfxRegistry.TextureStore.GetHandleAndMeta(textureId, out var meta);
        if (meta.IsMsaa) return;
        var wrapR = SupportsWrapR(meta.Kind);
        ApplyTextureProperties(texHandle, in meta, wrapR);
    }


    public void UploadTexture2D(TextureId textureId, ReadOnlySpan<byte> data, Size2D size)
    {
        var texHandle = GfxRegistry.TextureStore.GetHandleAndMeta(textureId, out var meta);
        if (meta.Kind == TextureKind.Unknown) throw new GraphicsException(nameof(meta.Kind));

        ValidateUploadSize(size, meta.AsSize2D());

        GlTextures.UploadTexture2D_Data(texHandle, data, meta.PixelFormat, size);
    }

    public void UploadTexture3D(TextureId textureId, ReadOnlySpan<byte> data, int width, int height, int depth)
    {
        var texHandle = GfxRegistry.TextureStore.GetHandleAndMeta(textureId, out var meta);
        if (meta.Kind != TextureKind.Texture3D) throw new GraphicsException(nameof(meta.Kind));

        var (size, metaSize) = (new Size3D(width, height, depth), new Size3D(meta.Width, meta.Height, meta.Depth));
        ValidateUploadSize3D(size, metaSize);

        GlTextures.UploadTexture3D_Data(texHandle, data, meta.PixelFormat, size, zOffset: 0);
    }

    public void UploadCubeMapFace(TextureId textureId, ReadOnlySpan<byte> data, Size2D size, int faceIndex)
    {
        ArgumentOutOfRangeException.ThrowIfZero(textureId.Id, nameof(textureId.Id));
        ArgumentOutOfRangeException.ThrowIfZero(data.Length, nameof(data.Length));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(faceIndex, 5);

        var texHandle = GfxRegistry.TextureStore.GetHandleAndMeta(textureId, out var meta);
        if (meta.Kind != TextureKind.CubeMap) throw new GraphicsException(nameof(meta.Kind));

        ValidateUploadSize(size, meta.AsSize2D());

        GlTextures.UploadTexture3D_Data(texHandle, data, meta.PixelFormat, size.ToSize3D(1), faceIndex);
    }

    public void GenerateMipMaps(TextureId textureId)
    {
        var texHandle = GfxRegistry.TextureStore.GetHandleAndMeta(textureId, out var meta);
        Debug.Assert(meta.MipLevels > 1);
        GlTextures.GenerateMipMaps(texHandle);
    }

    private void ApplyTextureProperties(NativeHandle texHandle, in TextureMeta meta, bool wrapR)
    {
        if (meta.Preset != TexturePreset.None)
            GlTextures.SetTexturePreset(texHandle, meta.Preset, wrapR);

        if (meta.CompareTextureFunc is not (DepthMode.Unset or DepthMode.None))
            GlTextures.SetCompareTextureFunc(texHandle, meta.CompareTextureFunc);

        if (meta.BorderColor.Enabled)
            GlTextures.SetBorder(texHandle, meta.BorderColor);

        if (meta.Anisotropy != TextureAnisotropy.Off)
            GlTextures.SetAnisotropy(texHandle, meta.Anisotropy.ToAnisotropy());

        if (meta.Lod != Half.Zero)
            GlTextures.SetLodBias(texHandle, (float)meta.Lod);

        if (meta.MipLevels > 1 && meta.Kind != TextureKind.Texture2DArray)
            GlTextures.GenerateMipMaps(texHandle);
    }

    private NativeHandle CreateDriverTexture(Size3D size, in CreateTextureProps props, out TextureMeta meta)
    {
        ValidateTextureDescriptor(size, props);
        GetMipValues(size, props.Preset, out var levels);
        if (levels < 1) throw new GraphicsException(nameof(levels));
        var samples = props.Samples.ToSamples();

        var texHandle = GlTextures.CreateTexture(props.Kind);

        switch (props.Kind)
        {
            case TextureKind.Texture2D:
                GlTextures.TextureStorage2D(texHandle, size, GpuTextureProps.Make(props.Format, levels, 0));
                break;
            case TextureKind.CubeMap:
                GlTextures.TextureStorage2D(texHandle, size, GpuTextureProps.Make(props.Format, levels, 0));
                break;
            case TextureKind.Multisample2D:
                var msaaStoreProps = GpuTextureProps.Make(props.Format, levels, props.Samples.ToSamples());
                GlTextures.TextureStorage2D_MultiSample(texHandle, size, msaaStoreProps);
                break;
            case TextureKind.Texture3D:
                var tex3DStoreProps = GpuTextureProps.Make(props.Format, levels, 0);
                GlTextures.TextureStorage3D(texHandle, size, tex3DStoreProps);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(props.Kind));
        }

        meta = new TextureMeta(
            size.Width, size.Height, (ushort)size.Depth, props.Lod,
            (byte)levels, (byte)samples, props.Preset, props.Kind, props.Anisotropy, props.Format,
            props.CompareTextureFunc, props.BorderColor
        );

        return texHandle;
    }
}