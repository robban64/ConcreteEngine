using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.Gfx.Types;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
using static ConcreteEngine.Graphics.Gfx.Internals.GfxTextureUtils;

namespace ConcreteEngine.Graphics.Gfx;

/*
    public TextureId BuildTexture2DArray(ReadOnlySpan<TextureId> textures, int totalLayers = 0)
    {
        ArgumentOutOfRangeException.ThrowIfZero(textures.Length);

        var layers = int.Max(textures.Length, totalLayers);

        var baseMeta = _textureStore.GetMeta(textures[0]);
        if (baseMeta.Kind != TextureKind.Texture2D) throw new GraphicsException(nameof(baseMeta.Kind));

        foreach (var texId in textures)
        {
            var handle = _textureStore.GetHandleAndMeta(texId, out var meta);

            if (meta.Kind != TextureKind.Texture2D) throw new GraphicsException(nameof(meta.Kind));
            if (baseMeta.PixelFormat != meta.PixelFormat) throw new GraphicsException(nameof(meta.PixelFormat));
            if (baseMeta.MipLevels != meta.MipLevels) throw new GraphicsException(nameof(meta.MipLevels));
            if (baseMeta.Width != meta.Width || baseMeta.Height != meta.Height)
                throw new GraphicsException("Mismatch texture size");
        }

        var dstHandle = _driver.CreateTexture(TextureKind.Texture2DArray);

        var gpuProps = new GpuTextureProps(baseMeta.PixelFormat, baseMeta.MipLevels, baseMeta.Samples);
        _driver.TextureStorage3D(dstHandle, new Size3D(baseMeta.Width, baseMeta.Height, layers), gpuProps);

        var size = new Size3D(baseMeta.Width, baseMeta.Height, 1);
        for (var layer = 0; layer < layers; layer++)
        {
            var texId = textures[layer];
            var srcHandle = _textureStore.GetHandle(texId);
            _driver.CopyTextureData(
                src: srcHandle,
                srcKind: TextureKind.Texture2D,
                dst: dstHandle,
                dstKind: TextureKind.Texture2DArray,
                srcLevel: 0,
                dstLevel: 0,
                srcSize: size,
                dstPos: new Vector3I(0, 0, layer)
            );
            for (int mip = 0; mip < baseMeta.MipLevels; mip++)
            {
                _driver.CopyTextureData(
                    src: srcHandle,
                    srcKind: TextureKind.Texture2D,
                    dst: dstHandle,
                    dstKind: TextureKind.Texture2DArray,
                    srcLevel: 0,
                    dstLevel: mip,
                    srcSize: GfxUtilsInternal.CalcMipSize(mip, size).ToSize3D(1),
                    dstPos: new Vector3I(0, 0, layer)
                );
            }
        }

        return _textureStore.Add(baseMeta, dstHandle);
    }
    */

public sealed class GfxTextures
{
    public static class Fallback
    {
        public static TextureId AlbedoId { get; internal set; }
        public static TextureId NormalId { get; internal set; }
        public static TextureId AlphaMaskId { get; internal set; }
    }

    private readonly GlTextures _driver;

    private readonly GfxResourceDisposer _disposer;
    private readonly GfxResourceStore<TextureId, TextureMeta> _textureStore;

    internal GfxTextures(GfxContextInternal context)
    {
        _disposer = context.Disposer;
        _textureStore = context.Resources.GfxStoreHub.TextureStore;
        _driver = context.Driver.Textures;

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
        var textRef = CreateDriverTexture(size, in props, out var meta);
        var textureId = _textureStore.Add(in meta, textRef);
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


    public unsafe TextureId CreateCubeMap(Size2D size, in CreateTextureProps props, NativeView<byte>* faces)
    {
        if (faces == null) throw new ArgumentNullException(nameof(faces));

        var textureId = CreateTexture(size.ToSize3D(1), in props);
        for (int i = 0; i < 6; i++)
        {
            if (faces + i == null || (faces + i)->IsNull)
                Throwers.NullPointer($"Null pointer for face {i}");

            UploadCubeMapFace(textureId, faces[i].AsSpan(), size, i);
        }

        ApplyProperties(textureId);
        return textureId;
    }

    public TextureId CreateTexture2DArrayFrom(TextureId baseTexId, int layers)
    {
        ArgumentOutOfRangeException.ThrowIfZero(baseTexId.Value, nameof(baseTexId));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(layers, 1);

        var baseMeta = _textureStore.GetMeta(baseTexId);
        if (baseMeta.Kind != TextureKind.Texture2D) throw new GraphicsException(nameof(baseMeta.Kind));

        var handle = _driver.CreateTexture(TextureKind.Texture2DArray);

        var gpuProps = new GpuTextureProps(baseMeta.PixelFormat, baseMeta.MipLevels, baseMeta.Samples);
        _driver.TextureStorage3D(handle, new Size3D(baseMeta.Width, baseMeta.Height, layers), gpuProps);

        var meta = baseMeta with { Kind = TextureKind.Texture2DArray, Depth = (ushort)layers };
        var textureId = _textureStore.Add(in meta, handle);
        
        ApplyProperties(textureId);
        return textureId;
    }

    public void SetTexture2DArrayLayerFrom(TextureId arrayId, TextureId srcId, int layer)
    {
        ArgumentOutOfRangeException.ThrowIfZero(arrayId.Value, nameof(arrayId));
        ArgumentOutOfRangeException.ThrowIfZero(srcId.Value, nameof(srcId));
        ArgumentOutOfRangeException.ThrowIfNegative(layer);

        var dstHandle = _textureStore.GetHandleAndMeta(arrayId, out var dstMeta);
        if (dstMeta.Depth < 2) throw new GraphicsException(nameof(dstMeta.MipLevels));
        if (dstMeta.Kind != TextureKind.Texture2DArray) throw new GraphicsException(nameof(dstMeta.Kind));

        var srcHandle = _textureStore.GetHandleAndMeta(srcId, out var srcMeta);
        if (srcMeta.Kind != TextureKind.Texture2D) throw new GraphicsException(nameof(srcMeta.Kind));

        if (dstMeta.PixelFormat != srcMeta.PixelFormat) throw new GraphicsException(nameof(srcMeta.PixelFormat));
        if (dstMeta.MipLevels != srcMeta.MipLevels) throw new GraphicsException(nameof(srcMeta.MipLevels));
        if (dstMeta.Width != srcMeta.Width || dstMeta.Height != srcMeta.Height)
            throw new GraphicsException("Mismatch texture size");

        var size = dstMeta.AsSize2D();
        for (int mip = 0; mip < dstMeta.MipLevels; mip++)
        {
            _driver.CopyTextureData(
                src: srcHandle,
                srcKind: TextureKind.Texture2D,
                dst: dstHandle,
                dstKind: TextureKind.Texture2DArray,
                srcLevel: mip,
                dstLevel: mip,
                srcSize: CalcMipSize(mip, size).ToSize3D(1),
                dstPos: new Vector3I(0, 0, layer)
            );
            
        }
    }

    internal GfxHandle ReplaceTexture(TextureId textureId, Size3D size, int? samples = null)
    {
        var texRef = _textureStore.GetHandleAndMeta(textureId, out var meta);
        _disposer.EnqueueReplace(texRef);

        samples = meta.Kind == TextureKind.Multisample2D ? samples ?? meta.Samples : samples;
        var msaa = GfxEnumUtils.ToRenderBufferMsaa(samples);

        ValidateRecreateTexture(size, samples, in meta);

        var props = new CreateTextureProps((float)meta.Lod, meta.Kind, meta.PixelFormat, meta.Preset, meta.Anisotropy,
            meta.CompareTextureFunc,
            meta.BorderColor, msaa);

        var newTexRef = CreateDriverTexture(size, in props, out var newMeta);
        _textureStore.Replace(textureId, in newMeta, in newTexRef, out _);
        return newTexRef;
    }

    public void ApplyProperties(TextureId textureId)
    {
        var texRef = _textureStore.GetHandleAndMeta(textureId, out var meta);
        if (meta.IsMsaa) return;
        var wrapR = SupportsWrapR(meta.Kind);
        ApplyTextureProperties(texRef, in meta, wrapR);
    }


    public void UploadTexture2D(TextureId textureId, ReadOnlySpan<byte> data, Size2D size)
    {
        var texRef = _textureStore.GetHandleAndMeta(textureId, out var meta);
        if (meta.Kind == TextureKind.Unknown) throw new GraphicsException(nameof(meta.Kind));

        ValidateUploadSize(size, meta.AsSize2D());

        _driver.UploadTexture2D_Data(texRef, data, meta.PixelFormat, size);
    }

    public void UploadTexture3D(TextureId textureId, ReadOnlySpan<byte> data, int width, int height, int depth)
    {
        var texRef = _textureStore.GetHandleAndMeta(textureId, out var meta);
        if (meta.Kind != TextureKind.Texture3D) throw new GraphicsException(nameof(meta.Kind));

        var (size, metaSize) = (new Size3D(width, height, depth), new Size3D(meta.Width, meta.Height, meta.Depth));
        ValidateUploadSize3D(size, metaSize);

        _driver.UploadTexture3D_Data(texRef, data, meta.PixelFormat, size, zOffset: 0);
    }

    public void GenerateMipMaps(TextureId textureId)
    {
        var texRef = _textureStore.GetHandleAndMeta(textureId, out var meta);
        Debug.Assert(meta.MipLevels > 1);
        _driver.GenerateMipMaps(texRef);
    }


    private void UploadCubeMapFace(TextureId textureId, ReadOnlySpan<byte> data, Size2D size, int faceIndex)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(faceIndex, 5);

        var texRef = _textureStore.GetHandleAndMeta(textureId, out var meta);
        if (meta.Kind != TextureKind.CubeMap) throw new GraphicsException(nameof(meta.Kind));

        ValidateUploadSize(size, meta.AsSize2D());

        _driver.UploadTexture3D_Data(texRef, data, meta.PixelFormat, size.ToSize3D(1), faceIndex);
    }

    private void ApplyTextureProperties(GfxHandle texRef, in TextureMeta meta, bool wrapR)
    {
        if (meta.Preset != TexturePreset.None)
            _driver.SetTexturePreset(texRef, meta.Preset, wrapR);

        if (meta.CompareTextureFunc is not (DepthMode.Unset or DepthMode.None))
            _driver.SetCompareTextureFunc(texRef, meta.CompareTextureFunc);

        if (meta.BorderColor.Enabled)
            _driver.SetBorder(texRef, meta.BorderColor);

        if (meta.Anisotropy != TextureAnisotropy.Off)
            _driver.SetAnisotropy(texRef, meta.Anisotropy.ToAnisotropy());

        if (meta.Lod != Half.Zero)
            _driver.SetLodBias(texRef, (float)meta.Lod);

        if (meta.MipLevels > 1 && meta.Kind  != TextureKind.Texture2DArray)
            _driver.GenerateMipMaps(texRef);
    }

    private GfxHandle CreateDriverTexture(Size3D size, in CreateTextureProps props, out TextureMeta meta)
    {
        ValidateTextureDescriptor(size, props);
        GetMipValues(size, props.Preset, out var levels);
        if (levels < 1) throw new GraphicsException(nameof(levels));
        var samples = props.Samples.ToSamples();

        var texRef = _driver.CreateTexture(props.Kind);

        switch (props.Kind)
        {
            case TextureKind.Texture2D:
                _driver.TextureStorage2D(texRef, size, GpuTextureProps.Make(props.Format, levels, 0));
                break;
            case TextureKind.CubeMap:
                _driver.TextureStorage2D(texRef, size, GpuTextureProps.Make(props.Format, levels, 0));
                break;
            case TextureKind.Multisample2D:
                var msaaStoreProps = GpuTextureProps.Make(props.Format, levels, props.Samples.ToSamples());
                _driver.TextureStorage2D_MultiSample(texRef, size, msaaStoreProps);
                break;
            case TextureKind.Texture3D:
                var tex3DStoreProps = GpuTextureProps.Make(props.Format, levels, 0);
                _driver.TextureStorage3D(texRef, size, tex3DStoreProps);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(props.Kind));
        }

        meta = new TextureMeta(
            size.Width, size.Height, (ushort)size.Depth, props.Lod,
            (byte)levels, (byte)samples, props.Preset, props.Kind, props.Anisotropy, props.Format,
            props.CompareTextureFunc, props.BorderColor
        );

        return texRef;
    }
}