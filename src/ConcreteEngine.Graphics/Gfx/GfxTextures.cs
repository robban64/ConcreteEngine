using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

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

    internal GfxHandle ReplaceTexture(TextureId textureId, Size3D size, int? samples = null)
    {
        var texRef = _textureStore.GetHandleAndMeta(textureId, out var meta);
        _disposer.EnqueueReplace(texRef);

        samples = meta.Kind == TextureKind.Multisample2D ? samples ?? meta.Samples : samples;
        var msaa = GfxUtilsEnum.ToRenderBufferMsaa(samples);

        ValidateRecreateTexture(size, samples, in meta);

        var props = new CreateTextureProps((float)meta.Lod, meta.Kind, meta.PixelFormat, meta.Preset, meta.Anisotropy,
            meta.CompareTextureFunc,
            meta.BorderColor, msaa);

        var newTexRef = CreateDriverTexture(size, in props, out var newMeta);
        _textureStore.Replace(textureId, in newMeta, in newTexRef, out _);
        return newTexRef;
    }


    // utilities
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


    public unsafe TextureId CreateCubeMap(Size2D size, in CreateTextureProps props,
        NativeView<byte>* faces)
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


    public  TextureId BuildTexture2DArray(ReadOnlySpan<TextureId> textures, int totalLayers = 0)
    {
        ArgumentOutOfRangeException.ThrowIfZero(textures.Length);

        var layers = int.Max(textures.Length, totalLayers);

        var baseMeta = _textureStore.GetMeta(textures[0]);
        if (baseMeta.Kind != TextureKind.Texture2D) throw new GraphicsException(nameof(baseMeta.Kind));

        foreach (var texId in textures)
        {
            var meta = _textureStore.GetMeta(texId);

            if (meta.Kind != TextureKind.Texture2D) throw new GraphicsException(nameof(meta.Kind));
            if (baseMeta.PixelFormat != meta.PixelFormat) throw new GraphicsException(nameof(meta.PixelFormat));
            if (baseMeta.Levels != meta.Levels) throw new GraphicsException(nameof(meta.Levels));
            if (baseMeta.Width != meta.Width || baseMeta.Height != meta.Height)
                throw new GraphicsException("Mismatch texture size");
        }

        var dstHandle = _driver.CreateTexture(TextureKind.Texture2DArray);

        var gpuProps = new GpuTextureProps(baseMeta.PixelFormat, baseMeta.Levels, baseMeta.Samples);
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
                dstPos: new Vector3I(0,0, layer)
            );
            for (int mip = 0; mip < baseMeta.Levels; mip++)
            {
                _driver.CopyTextureData(
                    src: srcHandle,
                    srcKind: TextureKind.Texture2D,
                    dst: dstHandle,
                    dstKind: TextureKind.Texture2DArray,
                    srcLevel: 0,
                    dstLevel: mip,
                    srcSize: GfxUtilsInternal.CalcMipSize(mip, size).ToSize3D(1),
                    dstPos: new Vector3I(0,0, layer)
                );
            }
        }

        return _textureStore.Add(baseMeta, dstHandle);
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

        //if (data.Length != meta.SizeInBytes)
        var newMeta = TextureMeta.CopyWithNewSize(in meta);
        _textureStore.ReplaceMeta(textureId, in newMeta, out _);
    }

    public void UploadTexture3D(TextureId textureId, ReadOnlySpan<byte> data, int width, int height, int depth)
    {
        var texRef = _textureStore.GetHandleAndMeta(textureId, out var meta);
        if (meta.Kind != TextureKind.Texture3D) throw new GraphicsException(nameof(meta.Kind));

        var (size, metaSize) = (new Size3D(width, height, depth), new Size3D(meta.Width, meta.Height, meta.Depth));
        ValidateUploadSize3D(size, metaSize);

        _driver.UploadTexture3D_Data(texRef, data, meta.PixelFormat, size, zOffset: 0); //add zOffset later if needed

        //if (data.Length != meta.SizeInBytes)
        var newMeta = TextureMeta.CopyWithNewSize(in meta);
        _textureStore.ReplaceMeta(textureId, in newMeta, out _);
    }

    public void GenerateMipMaps(TextureId textureId)
    {
        var texRef = _textureStore.GetHandleAndMeta(textureId, out var meta);
        Debug.Assert(meta.Levels > 1);
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

    private GfxHandle CreateDriverTexture(Size3D size, in CreateTextureProps props, out TextureMeta meta)
    {
        ValidateTextureDescriptor(size, props);
        var (mipPreset, levels) = GetMipValues(size, props.Preset);
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

        if (meta.Levels > 1)
            _driver.GenerateMipMaps(texRef);
    }

    private static bool SupportsWrapR(TextureKind kind) => kind is TextureKind.CubeMap or TextureKind.Texture3D;

    private static (bool mipPreset, int levels) GetMipValues(Size3D size, TexturePreset preset)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Depth, 1, nameof(size.Depth));
        var mipPreset = preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
        var levels = mipPreset ? GfxUtilsInternal.CalcMipLevels(size.Width, size.Height, size.Depth) : 1;
        return (mipPreset, levels);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateRecreateTexture(Size3D size, int? samples, in TextureMeta meta)
    {
        if (meta.Kind == TextureKind.Unknown || meta.PixelFormat == TexturePixelFormat.Unknown)
            throw new GraphicsException("Invalid meta texture meta.");

        if (meta.Kind == TextureKind.CubeMap && size.Width != size.Height)
            throw new GraphicsException("CubeMap must be square.");

        if (meta.Kind is TextureKind.Texture3D or TextureKind.Texture2DArray)
        {
            if (size.Depth <= 0) throw new GraphicsException("Texture3D/Texture2DArray require positive depth");
        }
        else if (size.Depth != 1)
        {
            throw new GraphicsException("Depth must be 1 for non-3D or 2DArray textures");
        }


        if (meta.Kind != TextureKind.Multisample2D && samples is not null)
            throw new GraphicsException("Samples can only be set for Multisample2D.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateTextureDescriptor(Size3D size, CreateTextureProps props)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(props.Kind, TextureKind.Unknown);
        ArgumentOutOfRangeException.ThrowIfEqual(props.Format, TexturePixelFormat.Unknown);

        if (size.IsZero() || size.AnyNegative())
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be > 0");

        // Depth
        if (props.Kind is TextureKind.Texture3D or TextureKind.Texture2DArray)
        {
            if (size.Depth <= 0) throw new GraphicsException("Texture3D/Texture2DArray require positive depth");
        }
        else if (size.Depth != 1)
        {
            throw new GraphicsException("Depth must be 1 for non-3D or 2DArray textures");
        }

        // MSAA
        bool isMsaa = props.Kind == TextureKind.Multisample2D;
        if (isMsaa && props.Samples == RenderBufferMsaa.None)
            throw new ArgumentException("Multisample2D must have MSAA != None", nameof(props.Samples));
        if (!isMsaa && props.Samples != RenderBufferMsaa.None)
            throw new ArgumentException("Non-multisample textures must have MSAA=None", nameof(props.Samples));

        // CubeMap
        if (props.Kind == TextureKind.CubeMap && size.Width != size.Height)
            throw new ArgumentException("CubeMap faces must be square (W==H)");


        (bool mipPreset, int levels) = GetMipValues(size, props.Preset);

        if (isMsaa && levels != 1)
            throw new GraphicsException("Multisample textures cannot have mipmaps");

        if (!mipPreset)
        {
            if (props.Anisotropy != TextureAnisotropy.Off)
                throw new GraphicsException("Anisotropy requires mipmaps");
            if ((float)props.Lod != 0f)
                throw new GraphicsException("LodBias requires mipmaps");
        }
    }

    private static void ValidateUploadSize(Size2D size, Size2D metaSize)
    {
        if (size.IsZero() || metaSize.IsNegative()) throw new ArgumentOutOfRangeException(nameof(size));
        if (size != metaSize)
            throw new GraphicsException($"Size {size} must match TextureMeta size {metaSize}");
    }

    private static void ValidateUploadSize3D(Size3D size, Size3D metaSize)
    {
        if (size.IsZero() || metaSize.AnyNegative()) throw new ArgumentOutOfRangeException(nameof(size));
        if (size != metaSize)
            throw new GraphicsException($"Size {size} must match TextureMeta size {metaSize}");
    }
}