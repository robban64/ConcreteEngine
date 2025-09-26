#region

using System.Diagnostics;
using System.Runtime.CompilerServices;
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

    private readonly GlTextures _driver;

    internal GfxTextures(GfxContextInternal context)
    {
        _resources = context.Stores;
        _repository = context.Repositories;
        _driver = context.Driver.Textures;
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
        var textRef = CreateTextureInternal(in desc, in props, out var meta);
        var textureId = _resources.TextureStore.Add(in meta, in textRef);
        return textureId;
    }

    public void ApplyProperties(TextureId textureId)
    {
        var texRef = _resources.TextureStore.GetRefAndMeta(textureId, out var meta);
        if (meta.IsMsaa) return;
        var wrapR = SupportsWrapR(meta.Kind);
        var applyProps = new TextureProperties(meta.Preset, meta.Anisotropy, meta.Levels, meta.Lod, wrapR);
        ApplyTextureProperties(texRef, applyProps);

        if (meta.IsMipMapped)
            GenerateMipMaps(textureId);
    }

    internal GfxRefToken<TextureId> ReplaceTexture(TextureId textureId, in GfxReplaceTexture newProps)
    {
        var meta = _resources.TextureStore.GetMeta(textureId);
        var samples = meta.Kind == TextureKind.Multisample2D ? newProps.Samples ?? meta.Samples : newProps.Samples;
        var msaa = GfxUtilsEnum.ToRenderBufferMsaa(samples);

        ValidateRecreateTexture(newProps, in meta);


        var desc = new GfxTextureDescriptor(newProps.Width, newProps.Height,
            meta.Kind, meta.PixelFormat, meta.Depth, msaa);
        var props = new GfxTextureProperties(meta.Preset, meta.Anisotropy, meta.Lod);
        var newTexRef = CreateTextureInternal(in desc, in props, out var newMeta);
        _resources.TextureStore.Replace(textureId, in newMeta, in newTexRef, out _);
        return newTexRef;
    }


    public void UploadTexture2D(TextureId textureId, ReadOnlySpan<byte> data, int width, int height)
    {
        var texRef = _resources.TextureStore.GetRefAndMeta(textureId, out var meta);
        if (meta.Kind == TextureKind.Unknown) throw new InvalidOperationException(nameof(meta.Kind));

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
        if (meta.Kind != TextureKind.Texture3D) throw new InvalidOperationException(nameof(meta.Kind));

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
        if (meta.Kind != TextureKind.CubeMap) throw new InvalidOperationException(nameof(meta.Kind));

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


    private GfxRefToken<TextureId> CreateTextureInternal(in GfxTextureDescriptor desc, in GfxTextureProperties props,
        out TextureMeta meta)
    {
        ValidateTextureDescriptor(in desc, in props);
        var size = new Size2D(desc.Width, desc.Height);
        (bool mipPreset, int levels) = GetMipValues(desc.Width, desc.Height, props.Preset, desc.Depth);
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
            desc.Width, desc.Height, props.Preset, desc.Kind, props.Anisotropy, desc.Format,
            props.LodBias, desc.Depth, (short)levels, (short)samples, 0
        );

        return texRef;
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