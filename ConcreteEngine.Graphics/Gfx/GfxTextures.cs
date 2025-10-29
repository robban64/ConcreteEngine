#region

using System.Diagnostics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxTextures
{
    private readonly GlTextures _driver;

    private readonly GfxResourceDisposer _disposer;
    private readonly TextureStore _textureStore;

    public static class FallbackTextures
    {
        public static TextureId AlbedoId { get; internal set; } = default;
        public static TextureId NormalId { get; internal set; } = default;
        public static TextureId AlphaMaskId { get; internal set; } = default;
    }

    internal GfxTextures(GfxContextInternal context)
    {
        _disposer = context.Disposer;
        _textureStore = context.Resources.GfxStoreHub.TextureStore;
        _driver = context.Driver.Textures;

        FallbackTextures.AlbedoId = CreateOnePixelTexture([255, 255, 255, 255], TexturePixelFormat.SrgbAlpha);
        FallbackTextures.NormalId = CreateOnePixelTexture([128, 128, 255], TexturePixelFormat.Rgb);
        FallbackTextures.AlphaMaskId
            = CreateOnePixelTexture([255], TexturePixelFormat.Depth, TexturePreset.NearestClamp);
    }

    private TextureId CreateOnePixelTexture(byte[] pixelData, TexturePixelFormat format,
        TexturePreset preset = TexturePreset.NearestRepeat)
    {
        var desc = new GfxTextureDescriptor(1, 1, TextureKind.Texture2D, format);
        var props = new GfxTextureProperties(0, preset, TextureAnisotropy.Off);
        return BuildTexture(desc, props, pixelData);
    }


    // utilities
    public TextureId BuildTexture(in GfxTextureDescriptor desc, in GfxTextureProperties props,
        ReadOnlySpan<byte> data)
    {
        var textureId = CreateTexture(in desc, in props);
        UploadTexture2D(textureId, data, desc.Width, desc.Height);
        ApplyProperties(textureId);
        return textureId;
    }

    public TextureId BuildCubeMap(in GfxTextureDescriptor desc, in GfxTextureProperties props,
        ReadOnlyMemory<byte>[] faces)
    {
        var textureId = CreateTexture(in desc, in props);
        for (int i = 0; i < 6; i++)
        {
            UploadCubeMapFace(textureId, faces[i].Span, desc.Width, desc.Height, i);
        }

        ApplyProperties(textureId);
        return textureId;
    }

    public TextureId BuildEmptyTexture(in GfxTextureDescriptor desc, in GfxTextureProperties props)
    {
        var textureId = CreateTexture(in desc, in props);
        ApplyProperties(textureId);
        return textureId;
    }


    public TextureId CreateTexture(in GfxTextureDescriptor desc, in GfxTextureProperties props)
    {
        var textRef = CreateDriverTexture(in desc, in props, out var meta);
        var textureId = _textureStore.Add(in meta, in textRef);
        return textureId;
    }

    public void ApplyProperties(TextureId textureId)
    {
        var texRef = _textureStore.GetRefAndMeta(textureId, out var meta);
        if (meta.IsMsaa) return;
        var wrapR = SupportsWrapR(meta.Kind);
        ApplyTextureProperties(texRef, in meta, wrapR);
    }

    internal GfxRefToken<TextureId> ReplaceTexture(TextureId textureId, in GfxReplaceTexture newProps)
    {
        var texRef = _textureStore.GetRefAndMeta(textureId, out var meta);
        _disposer.EnqueueReplace(texRef);

        var samples = meta.Kind == TextureKind.Multisample2D ? newProps.Samples ?? meta.Samples : newProps.Samples;
        var msaa = GfxUtilsEnum.ToRenderBufferMsaa(samples);

        ValidateRecreateTexture(newProps, in meta);

        var desc = new GfxTextureDescriptor(newProps.Width, newProps.Height,
            meta.Kind, meta.PixelFormat, meta.Depth, msaa);

        var props = new GfxTextureProperties((float)meta.Lod, meta.Preset, meta.Anisotropy, meta.CompareTextureFunc,
            meta.BorderColor);

        var newTexRef = CreateDriverTexture(in desc, in props, out var newMeta);
        _textureStore.Replace(textureId, in newMeta, in newTexRef, out _);
        return newTexRef;
    }


    public void UploadTexture2D(TextureId textureId, ReadOnlySpan<byte> data, int width, int height)
    {
        var texRef = _textureStore.GetRefAndMeta(textureId, out var meta);
        if (meta.Kind == TextureKind.Unknown) throw new InvalidOperationException(nameof(meta.Kind));

        var (size, metaSize) = (new Size2D(width, height), new Size2D(meta.Width, meta.Height));
        ValidateUploadSize(size, metaSize);

        _driver.UploadTexture2D_Data(texRef, data, meta.PixelFormat, size);

        //if (data.Length != meta.SizeInBytes)
        var newMeta = TextureMeta.CopyWithNewSize(in meta);
        _textureStore.ReplaceMeta(textureId, in newMeta, out _);
    }

    public void UploadTexture3D(TextureId textureId, ReadOnlySpan<byte> data, int width, int height, int depth)
    {
        var texRef = _textureStore.GetRefAndMeta(textureId, out var meta);
        if (meta.Kind != TextureKind.Texture3D) throw new InvalidOperationException(nameof(meta.Kind));

        var (size, metaSize) = (new Size3D(width, height, depth), new Size3D(meta.Width, meta.Height, meta.Depth));
        ValidateUploadSize3D(size, metaSize);

        _driver.UploadTexture3D_Data(texRef, data, meta.PixelFormat, size, zOffset: 0); //add zOffset later if needed

        //if (data.Length != meta.SizeInBytes)
        var newMeta = TextureMeta.CopyWithNewSize(in meta);
        _textureStore.ReplaceMeta(textureId, in newMeta, out _);
    }

    public void UploadCubeMapFace(TextureId textureId, ReadOnlySpan<byte> data, int width, int height, int faceIdx)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(faceIdx, 5, nameof(faceIdx));

        var texRef = _textureStore.GetRefAndMeta(textureId, out var meta);
        if (meta.Kind != TextureKind.CubeMap) throw new InvalidOperationException(nameof(meta.Kind));

        var (size, metaSize) = (new Size2D(width, height), new Size2D(meta.Width, meta.Height));
        ValidateUploadSize(size, metaSize);

        _driver.UploadTexture3D_Data(texRef, data, meta.PixelFormat, new Size3D(width, height, 1), faceIdx);

        if (faceIdx == 5)
        {
            var newMeta = TextureMeta.CopyWithNewSize(in meta);
            _textureStore.ReplaceMeta(textureId, in newMeta, out _);
        }
    }

    public void GenerateMipMaps(TextureId textureId)
    {
        var texRef = _textureStore.GetRefAndMeta(textureId, out var meta);
        Debug.Assert(meta.Levels > 1);
        _driver.GenerateMipMaps(texRef);
    }


    private GfxRefToken<TextureId> CreateDriverTexture(in GfxTextureDescriptor desc, in GfxTextureProperties props,
        out TextureMeta meta)
    {
        ValidateTextureDescriptor(in desc, in props);
        var size = new Size2D(desc.Width, desc.Height);
        var (mipPreset, levels) = GetMipValues(desc.Width, desc.Height, props.Preset, desc.Depth);
        if (levels < 1) throw new InvalidOperationException(nameof(levels));
        var samples = desc.Samples.ToSamples();

        var texRef = _driver.CreateTexture(desc.Kind);

        switch (desc.Kind)
        {
            case TextureKind.Texture2D:
                _driver.TextureStorage2D(texRef, size, BkTextureStoreDesc.Make(desc.Format, levels, 0));
                break;
            case TextureKind.CubeMap:
                _driver.TextureStorage2D(texRef, size, BkTextureStoreDesc.Make(desc.Format, levels, 0));
                break;
            case TextureKind.Multisample2D:
                var msaaStoreProps = BkTextureStoreDesc.Make(desc.Format, levels, desc.Samples.ToSamples());
                _driver.TextureStorage2D_MultiSample(texRef, size, msaaStoreProps);
                break;
            case TextureKind.Texture3D:
                var tex3dStoreProps = BkTextureStoreDesc.Make(desc.Format, levels, 0);
                _driver.TextureStorage3D(texRef, Size3D.From(size, desc.Depth), tex3dStoreProps);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(desc));
        }

        meta = new TextureMeta(
            (Half)props.LodBias, (ushort)desc.Width, (ushort)desc.Height, (ushort)desc.Depth,
            (byte)levels, (byte)samples, props.Preset, desc.Kind, props.Anisotropy, desc.Format,
            props.CompareTextureFunc, props.BorderColor
        );

        return texRef;
    }

    private void ApplyTextureProperties(GfxRefToken<TextureId> texRef, in TextureMeta meta, bool wrapR)
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

    private static void ValidateRecreateTexture(GfxReplaceTexture newValue, in TextureMeta meta)
    {
        if (meta.Kind == TextureKind.Unknown || meta.PixelFormat == TexturePixelFormat.Unknown)
            throw new InvalidOperationException("Invalid meta texture meta.");

        if (meta.Kind == TextureKind.CubeMap && newValue.Width != newValue.Height)
            throw new ArgumentException("CubeMap must be square.");

        var depth = newValue.Depth ?? meta.Depth;
        if (meta.Kind == TextureKind.Texture3D)
        {
            if (depth <= 0)
                throw new ArgumentOutOfRangeException(nameof(depth));
        }
        else
        {
            if (depth != 1)
                throw new ArgumentException("Depth must be 1 for non-3D textures");
        }

        if (meta.Kind != TextureKind.Multisample2D && newValue.Samples is not null)
            throw new ArgumentException("Samples can only be set for Multisample2D.");
    }

    private static void ValidateTextureDescriptor(in GfxTextureDescriptor desc, in GfxTextureProperties props)
    {
        if (desc.Width <= 0 || desc.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(desc), "Size must be > 0");

        // Depth
        if (desc.Kind == TextureKind.Texture3D)
        {
            if (desc.Depth <= 0)
                throw new ArgumentOutOfRangeException(nameof(desc.Depth));
        }
        else
        {
            if (desc.Depth != 1)
                throw new ArgumentException("Depth must be 1 for non-3D textures");
        }

        // Type
        if (desc.Kind == TextureKind.Unknown) throw new ArgumentOutOfRangeException(nameof(desc.Kind));
        if (desc.Format == TexturePixelFormat.Unknown) throw new ArgumentOutOfRangeException(nameof(desc.Format));

        // MSAA
        bool isMsaa = desc.Kind == TextureKind.Multisample2D;
        if (isMsaa && desc.Samples == RenderBufferMsaa.None)
            throw new ArgumentException("Multisample2D must have MSAA != None", nameof(desc.Samples));
        if (!isMsaa && desc.Samples != RenderBufferMsaa.None)
            throw new ArgumentException("Non-multisample textures must have MSAA=None", nameof(desc.Samples));

        // CubeMap
        if (desc.Kind == TextureKind.CubeMap && desc.Width != desc.Height)
            throw new ArgumentException("CubeMap faces must be square (W==H)");


        (bool mipPreset, int levels) = GetMipValues(desc.Width, desc.Height, props.Preset, desc.Depth);

        if (isMsaa && levels != 1)
            throw new InvalidOperationException("Multisample textures cannot have mipmaps");

        if (!mipPreset)
        {
            if (props.Anisotropy != TextureAnisotropy.Off)
                throw new InvalidOperationException("Anisotropy requires mipmaps");
            if (props.LodBias != 0f)
                throw new InvalidOperationException("LodBias requires mipmaps");
        }
    }

    private static void ValidateUploadSize(Size2D size, Size2D metaSize)
    {
        if (size.IsZero() || metaSize.IsNegative()) throw new ArgumentOutOfRangeException(nameof(size));
        if (size != metaSize)
            throw new InvalidOperationException($"Size {size} must match TextureMeta size {metaSize}");
    }

    private static void ValidateUploadSize3D(Size3D size, Size3D metaSize)
    {
        if (size.IsZero() || metaSize.IsNegative()) throw new ArgumentOutOfRangeException(nameof(size));
        if (size != metaSize)
            throw new InvalidOperationException($"Size {size} must match TextureMeta size {metaSize}");
    }

    private static bool SupportsWrapR(TextureKind kind) => kind is TextureKind.CubeMap or TextureKind.Texture3D;

    private static (bool mipPreset, int levels) GetMipValues(int width, int height, TexturePreset preset, int depth = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(depth, 1, nameof(depth));
        var mipPreset = preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
        var levels = mipPreset ? GfxUtilsInternal.CalcMipLevels(width, height, height) : 1;
        return (mipPreset, levels);
    }
}