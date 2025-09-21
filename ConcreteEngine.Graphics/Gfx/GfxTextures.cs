using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxTextures
{
    private readonly GfxStoreHub _resources;
    private readonly GfxResourceRepository _repository;
    private readonly GfxTexturesBackend _backend;


    internal GfxTextures(GfxContextInternal context)
    {
        _backend = new GfxTexturesBackend(context);
        _resources = context.Stores;
        _repository = context.Repositories;
    }

    private GfxRefToken<TextureId> CreateTextureInternal(
        ReadOnlySpan<byte> data,
        in GpuTextureDescriptor desc,
        RenderBufferMsaa msaa,
        out TextureMeta meta)
    {
        var hasMip = desc.Preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
        var levels = hasMip ? GfxUtils.CalcMipLevels(desc.Width, desc.Height) : 1;

        var texRef = _backend.CreateTexture(in desc, levels, msaa);
        
        if(desc.Kind != TextureKind.Multisample2D && desc.Kind != TextureKind.CubeMap)
            _backend.UploadTextureData(texRef, data, desc.Width, desc.Height);

        if (desc.Kind != TextureKind.Multisample2D)
            _backend.ApplyTextureParams(texRef, desc.Preset, desc.Anisotropy, desc.LodBias, levels);

        meta = new TextureMeta(desc.Width, desc.Height, desc.Preset, desc.Kind, desc.Anisotropy, desc.Format,
            (byte)levels, !data.IsEmpty);

        return texRef;
    }

    public TextureId CreateTexture2D(ReadOnlySpan<byte> data, in GpuTextureDescriptor desc)
    {
        if (desc.Kind != TextureKind.Texture2D) throw new ArgumentOutOfRangeException(nameof(desc.Kind));
        var texRef = CreateTextureInternal(data, in desc, RenderBufferMsaa.None, out var meta);
        return _resources.TextureStore.Add(in meta, in texRef);
    }

    public TextureId CreateCubeMap(in GpuTextureDescriptor desc)
    {
        if (desc.Kind != TextureKind.CubeMap) throw new ArgumentOutOfRangeException(nameof(desc.Kind));

        ArgumentOutOfRangeException.ThrowIfNotEqual((int)desc.Kind, (int)TextureKind.CubeMap, nameof(desc.Kind));
        ArgumentOutOfRangeException.ThrowIfNotEqual(desc.Width, desc.Height, nameof(desc.Width));

        var texRef = CreateTextureInternal(ReadOnlySpan<byte>.Empty, in desc, RenderBufferMsaa.None, out var meta);
        return _resources.TextureStore.Add(in meta, in texRef);
    }

    public TextureId CreateTextureMsaa(in GpuTextureDescriptor desc, RenderBufferMsaa msaa)
    {
        if (desc.Kind != TextureKind.Multisample2D) throw new ArgumentOutOfRangeException(nameof(desc.Kind));
        ArgumentOutOfRangeException.ThrowIfEqual((int)msaa, (int)RenderBufferMsaa.None, nameof(msaa));

        var texRef = CreateTextureInternal(ReadOnlySpan<byte>.Empty, in desc, msaa, out var meta);
        return _resources.TextureStore.Add(in meta, in texRef);
    }


    internal TextureId ReplaceTexture(TextureId textureId, ReadOnlySpan<byte> data,
        in GpuTextureDescriptor desc, out GfxRefToken<TextureId> newTexRef)
    {
        if (desc.Kind == TextureKind.CubeMap && desc.Width != desc.Height)
            throw new ArgumentOutOfRangeException(nameof(desc.Width));

        newTexRef = CreateTextureInternal(data, in desc, RenderBufferMsaa.None, out var meta);
        return _resources.TextureStore.Replace(textureId, in meta, newTexRef, out _);
    }


    public void UploadTextureData(TextureId textureId, ReadOnlySpan<byte> data, int width, int height)
    {
        var texRef = _resources.TextureStore.GetRefAndMeta(textureId, out var meta);
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Width, width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Height, height, nameof(height));

        _backend.UploadTextureData(texRef, data, width, height);
        var newMeta = TextureMeta.CreateFromHasData(in meta, true);
        _resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);
    }

    public void UploadCubeMapFace(TextureId textureId, ReadOnlySpan<byte> data, int width, int height, int faceIdx)
    {
        var texRef = _resources.TextureStore.GetRefAndMeta(textureId, out var meta);

        ArgumentOutOfRangeException.ThrowIfNotEqual(width, height, nameof(width));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(faceIdx, 5, nameof(faceIdx));
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Width, width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Height, height, nameof(height));

        _backend.UploadCubeMapFace(texRef, data, width, height, faceIdx);

        if (faceIdx == 5)
        {
            var newMeta = TextureMeta.CreateFromHasData(in meta, true);
            _resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);
        }
    }

    private sealed class GfxTexturesBackend
    {
        private readonly GlTextures _driver;

        internal GfxTexturesBackend(GfxContextInternal context)
        {
            _driver = context.Driver.Textures;
        }

        public GfxRefToken<TextureId> CreateTexture(in GpuTextureDescriptor desc, int mipLevels,
            RenderBufferMsaa msaa = RenderBufferMsaa.None)
        {
            var samples = msaa.ToSamples();
            var (width, height) = (desc.Width, desc.Height);
            var texRef = desc.Kind switch
            {
                TextureKind.Texture2D => _driver.CreateTexture2D(width, height, mipLevels),
                TextureKind.CubeMap => _driver.CreateTextureCubeMap(width, height, mipLevels),
                TextureKind.Multisample2D => _driver.CreateTextureMultisample(width, height, samples),
                _ => throw new ArgumentOutOfRangeException()
            };
            return texRef;
        }


        public void ApplyTextureParams(GfxRefToken<TextureId> texRef, TexturePreset preset,
            TextureAnisotropy anisotropy, float lodBias, int levels)
        {
            if (levels > 1 && (preset != TexturePreset.LinearMipmapClamp && preset != TexturePreset.LinearMipmapRepeat))
                throw new ArgumentOutOfRangeException(nameof(preset));

            if (preset != TexturePreset.None)
                _driver.SetTexturePreset(texRef, preset);

            if (anisotropy != TextureAnisotropy.Off)
                _driver.SetAnisotropy(texRef, anisotropy.ToAnisotropy());

            if (lodBias != 0)
                _driver.SetLodBias(texRef, lodBias);

            if (levels > 0)
                _driver.GenerateMipMaps(texRef);
        }

        public void UploadTextureData(GfxRefToken<TextureId> texRef, ReadOnlySpan<byte> data, int width, int height) =>
            _driver.UploadTextureData(texRef, data, width, height);

        public void UploadTextureEmptyData(GfxRefToken<TextureId> texRef) =>
            _driver.UploadTextureEmptyData(texRef);


        public void UploadCubeMapFace(GfxRefToken<TextureId> texRef, ReadOnlySpan<byte> data, int width, int height,
            int faceIdx)
        {
            _driver.UploadCubeMapFaceData(texRef, data, width, height, faceIdx);
        }
    }
}