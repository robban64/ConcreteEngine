#region

using System.Diagnostics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxTextures
{
    private readonly GfxStoreHub _resources;
    private readonly GfxResourceRepository _repository;
    private readonly GfxTexturesBackend _backend;

    private readonly GlTextures _driver;

    internal GfxTextures(GfxContextInternal context)
    {
        _resources = context.Stores;
        _repository = context.Repositories;
        _driver = context.Driver.Textures;
        _backend = new GfxTexturesBackend(_driver);
    }

    private GfxRefToken<TextureId> CreateTextureInternal(GfxTextureDescriptor desc, GfxTextureProperties props,
        out TextureMeta meta)
    {
        ValidateTextureDescriptor(in desc, in props);
        var size = new Size2D(desc.Width, desc.Height);
        (bool mipPreset, int levels) = GetMipValues(desc.Width, desc.Height, props.Preset, desc.Depth);
        var wrapR = SupportsWrapR(desc.Kind);
        var samples = desc.Msaa.ToSamples();

        var texRef = _driver.CreateTexture(desc.Kind);

        switch (desc.Kind)
        {
            case TextureKind.Texture2D:
                _driver.TextureStorage2D(texRef, size, BkTextureStoreDesc.Make(desc.Format, levels));
                break;
            case TextureKind.CubeMap:
                _driver.TextureStorage2D(texRef, size, BkTextureStoreDesc.Make(desc.Format, levels));
                break;
            case TextureKind.Multisample2D:
                var msaaStoreProps = BkTextureStoreDesc.MakeMultiSample(desc.Format, desc.Msaa);
                _driver.TextureStorage2D_MultiSample(texRef, size, msaaStoreProps);
                break;
            case TextureKind.Texture3D:
                var tex3dStoreProps = BkTextureStoreDesc.MakeMultiSample(desc.Format, desc.Msaa);
                _driver.TextureStorage3D(texRef, Size3D.From(size, desc.Depth), tex3dStoreProps);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(desc));
        }

        var applyProps = new TextureProperties(props.Preset, props.Anisotropy, levels, props.LodBias, wrapR);
        ApplyTextureProperties(texRef, applyProps);

        meta = new TextureMeta(
            desc.Width, desc.Height,
            props.Preset, desc.Kind, props.Anisotropy, desc.Format,
            desc.Depth, (short)levels, (short)samples, 0
        );

        return texRef;
    }

    public TextureId CreateTexture(GfxTextureDescriptor desc, GfxTextureProperties props)
    {
        var textRef = CreateTextureInternal(desc, props, out var meta);
        return _resources.TextureStore.Add(in meta, in textRef);
    }

    internal GfxRefToken<TextureId> ReplaceTexture(TextureId textureId, ReadOnlySpan<byte> data,
        in GfxReplaceTexture newProps)
    {
        var texRef = _resources.TextureStore.GetRefAndMeta(textureId, out var meta);
        ValidateRecreateTexture(newProps, in meta)

        if (desc.Kind == TextureKind.CubeMap && desc.Width != desc.Height)
            throw new ArgumentOutOfRangeException(nameof(desc.Width));


        var newTexRef = CreateTextureInternal(desc, props, out var newMeta);
        var newTextureId = _resources.TextureStore.Replace(textureId, in newMeta, in newTexRef, out _);
        InvalidOpThrower.ThrowIfNot(textureId == newTextureId, nameof(textureId));
    }


    public void UploadTexture2D(TextureId textureId, ReadOnlySpan<byte> data, int width, int height)
    {
        _resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);

        var texRef = _resources.TextureStore.GetRefAndMeta(textureId, out var meta);
        var (size, metaSize) = (new Size2D(width, height), new Size2D(meta.Width, meta.Height));
        ValidateUploadSize(size, metaSize);

        _driver.UploadTexture2D_Data(texRef, data, meta.PixelFormat, size);

        if (data.Length != meta.SizeInBytes)
        {
            var newMeta = TextureMeta.CopyWithNewSize(in meta, data.Length);
            _resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);
        }
    }

    public void UploadTexture3D(TextureId textureId, ReadOnlySpan<byte> data, int width, int height, int depth)
    {
        var texRef = _resources.TextureStore.GetRefAndMeta(textureId, out var meta);
        var (size, metaSize) = (new Size3D(width, height, depth), new Size3D(meta.Width, meta.Height, meta.Depth));
        ValidateUploadSize3D(size, metaSize);

        _driver.UploadTexture3D_Data(texRef, data, meta.PixelFormat, size, zOffset: 0); //add zOffset later if needed

        if (data.Length != meta.SizeInBytes)
        {
            var newMeta = TextureMeta.CopyWithNewSize(in meta, data.Length);
            _resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);
        }
    }

    public void UploadCubeMapFace(TextureId textureId, ReadOnlySpan<byte> data, int width, int height, int faceIdx)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(faceIdx, 5, nameof(faceIdx));

        var texRef = _resources.TextureStore.GetRefAndMeta(textureId, out var meta);
        var (size, metaSize) = (new Size2D(width, height), new Size2D(meta.Width, meta.Height));
        ValidateUploadSize(size, metaSize);


        _driver.UploadTexture3D_Data(texRef, data, meta.PixelFormat, new Size3D(width, height, 1), faceIdx);

        if (faceIdx == 5)
        {
            var newMeta = TextureMeta.CopyWithNewSize(in meta, data.Length);
            _resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);
        }
    }

    public void GenerateMipMaps(TextureId textureId)
    {
        var texRef = _resources.TextureStore.GetRefAndMeta(textureId, out var meta);
        Debug.Assert(meta.Levels > 1);
        _driver.GenerateMipMaps(texRef);
    }

    private void ApplyTextureProperties(GfxRefToken<TextureId> texRef, TextureProperties props)
    {
        if (props.Preset != TexturePreset.None)
            _driver.SetTexturePreset(texRef, props.Preset, props.WrapR);

        if (props.Anisotropy != TextureAnisotropy.Off)
            _driver.SetAnisotropy(texRef, props.Anisotropy.ToAnisotropy());

        if (props.LodBias != 0)
            _driver.SetLodBias(texRef, props.LodBias);

        if (props.Levels > 1)
            _driver.GenerateMipMaps(texRef);
    }

    private void ValidateRecreateTexture(GfxReplaceTexture newValue, in TextureMeta meta)
    {
        if (meta.Kind == TextureKind.Unknown || meta.PixelFormat == EnginePixelFormat.Unknown)
            throw new InvalidOperationException("Invalid meta texture meta.");

        if (meta.Kind == TextureKind.CubeMap && newValue.Width != newValue.Height)
            throw new ArgumentException("CubeMap must be square.");

        if (meta.Kind != TextureKind.Texture3D && newValue.Depth != 1)
            throw new ArgumentException("Depth must be 1 for non-3D.");


        if (meta.Kind == TextureKind.Multisample2D)
        {
            if (newValue.Samples is null)
                newValue = newValue with { Samples = meta.Samples };
        }
        else if (newValue.Samples is not null)
            throw new ArgumentException("Samples can only be set for Multisample2D.");
    }

    private void ValidateTextureDescriptor(in GfxTextureDescriptor desc, in GfxTextureProperties props)
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
        if (desc.Format == EnginePixelFormat.Unknown) throw new ArgumentOutOfRangeException(nameof(desc.Format));

        // MSAA
        bool isMsaa = desc.Kind == TextureKind.Multisample2D;
        if (isMsaa && desc.Msaa == RenderBufferMsaa.None)
            throw new ArgumentException("Multisample2D must have MSAA != None", nameof(desc.Msaa));
        if (!isMsaa && desc.Msaa != RenderBufferMsaa.None)
            throw new ArgumentException("Non-multisample textures must have MSAA=None", nameof(desc.Msaa));

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

    private void ValidateUploadSize(Size2D size, Size2D metaSize)
    {
        if (size.IsZero() || metaSize.IsNegative()) throw new ArgumentOutOfRangeException(nameof(size));
        if (size != metaSize)
            throw new InvalidOperationException($"Size {size} must match TextureMeta size {metaSize}");
    }

    private void ValidateUploadSize3D(Size3D size, Size3D metaSize)
    {
        if (size.IsZero() || metaSize.IsNegative()) throw new ArgumentOutOfRangeException(nameof(size));
        if (size != metaSize)
            throw new InvalidOperationException($"Size {size} must match TextureMeta size {metaSize}");
    }


    private readonly record struct TextureProperties(
        TexturePreset Preset,
        TextureAnisotropy Anisotropy,
        int Levels,
        float LodBias,
        bool WrapR);

    private static bool SupportsWrapR(TextureKind kind) => kind is TextureKind.CubeMap or TextureKind.Texture3D;

    private static (bool mipPreset, int levels) GetMipValues(int width, int height, TexturePreset preset, int depth = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(depth, 1, nameof(depth));
        var mipPreset = preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
        var levels = mipPreset ? GfxUtilsInternal.CalcMipLevels(width, height, height) : 1;
        return (mipPreset, levels);
    }


    /*
    private GfxRefToken<TextureId> CreateTextureInternal(
        ReadOnlySpan<byte> data,
        in GfxTextureDescriptor desc,
        RenderBufferMsaa msaa,
        out TextureMeta meta)
    {
        var hasMip = desc.Preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
        var levels = hasMip ? GfxUtilsInternal.CalcMipLevels(desc.Width, desc.Height) : 1;

        var texRef = _backend.CreateTexture(in desc, levels, msaa);

        if (desc.Kind == TextureKind.Texture2D)
            _driver.UploadTexture2D_Data(texRef, data, desc.Width, desc.Height);
        else if (desc.Kind == TextureKind.Texture3D)
            _driver.UploadTexture3D_Data(texRef, ReadOnlySpan<byte>.Empty, desc.Width, desc.Height, desc.Depth);

        if (desc.Kind != TextureKind.Multisample2D)
            _backend.ApplyTextureParams(texRef, desc.Preset, desc.Kind, desc.Anisotropy, desc.LodBias, levels);

        meta = new TextureMeta(desc.Width, desc.Height, desc.Preset, desc.Kind, desc.Anisotropy, desc.Format,
            (byte)levels, !data.IsEmpty);

        return texRef;
    }

    public TextureId CreateTexture2D(ReadOnlySpan<byte> data, in GfxTextureDescriptor desc)
    {
        if (desc.Kind != TextureKind.Texture2D) throw new ArgumentOutOfRangeException(nameof(desc.Kind));
        var texRef = CreateTextureInternal(data, in desc, RenderBufferMsaa.None, out var meta);
        return _resources.TextureStore.Add(in meta, in texRef);
    }

    public TextureId CreateTexture3D(ReadOnlySpan<byte> data, in GfxTextureDescriptor desc)
    {
        if (desc.Kind != TextureKind.Texture3D) throw new ArgumentOutOfRangeException(nameof(desc.Kind));
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.Depth, 1);
        var texRef = CreateTextureInternal(data, in desc, RenderBufferMsaa.None, out var meta);
        return _resources.TextureStore.Add(in meta, in texRef);
    }

    public TextureId CreateCubeMap(in GfxTextureDescriptor desc)
    {
        if (desc.Kind != TextureKind.CubeMap) throw new ArgumentOutOfRangeException(nameof(desc.Kind));

        ArgumentOutOfRangeException.ThrowIfNotEqual((int)desc.Kind, (int)TextureKind.CubeMap, nameof(desc.Kind));
        ArgumentOutOfRangeException.ThrowIfNotEqual(desc.Width, desc.Height, nameof(desc.Width));

        var texRef = CreateTextureInternal(ReadOnlySpan<byte>.Empty, in desc, RenderBufferMsaa.None, out var meta);
        return _resources.TextureStore.Add(in meta, in texRef);
    }

    public TextureId CreateTextureMsaa(in GfxTextureDescriptor desc, RenderBufferMsaa msaa)
    {
        if (desc.Kind != TextureKind.Multisample2D) throw new ArgumentOutOfRangeException(nameof(desc.Kind));
        ArgumentOutOfRangeException.ThrowIfEqual((int)msaa, (int)RenderBufferMsaa.None, nameof(msaa));

        var texRef = CreateTextureInternal(ReadOnlySpan<byte>.Empty, in desc, msaa, out var meta);
        return _resources.TextureStore.Add(in meta, in texRef);
    }
*/


/*

    private sealed class GfxTexturesBackend
    {
        private readonly GlTextures _driver;

        internal GfxTexturesBackend(GlTextures driver)
        {
            _driver = driver;
        }

        public GfxRefToken<TextureId> CreateTexture(in GfxTextureDescriptor desc, int mipLevels,
            RenderBufferMsaa msaa = RenderBufferMsaa.None)
        {
            var samples = msaa.ToSamples();
            var (width, height) = (desc.Width, desc.Height);
            var texRef = desc.Kind switch
            {
                TextureKind.Texture2D => _driver.CreateTexture2D(width, height, mipLevels, desc.Format),
                TextureKind.Texture3D => _driver.CreateTexture3D(width, height, desc.Depth, mipLevels, desc.Format),
                TextureKind.CubeMap => _driver.CreateTextureCubeMap(width, height, mipLevels),
                TextureKind.Multisample2D => _driver.CreateTextureMultisample(width, height, samples),
                _ => throw new ArgumentOutOfRangeException()
            };
            return texRef;
        }


        public void ApplyTextureParams(GfxRefToken<TextureId> texRef, TexturePreset preset, TextureKind kind,
            TextureAnisotropy anisotropy, float lodBias, int levels)
        {
            if (levels > 1 && preset != TexturePreset.LinearMipmapClamp && preset != TexturePreset.LinearMipmapRepeat)
                throw new ArgumentOutOfRangeException(nameof(preset));

            if (preset != TexturePreset.None)
                _driver.SetTexturePreset(texRef, preset, kind);

            if (anisotropy != TextureAnisotropy.Off)
                _driver.SetAnisotropy(texRef, anisotropy.ToAnisotropy());

            if (lodBias != 0)
                _driver.SetLodBias(texRef, lodBias);

            if (levels > 1)
                _driver.GenerateMipMaps(texRef);
        }
    }
    */
}