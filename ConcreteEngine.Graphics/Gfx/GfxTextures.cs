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
    
    private TextureId CreateTextureInternal(ReadOnlySpan<byte> data, in GpuTextureDescriptor desc)
    {
        var texRef = _backend.CreateTexture(in desc);
        if (desc.Kind != TextureKind.Multisample2D)
        {
            _backend.UploadTextureData(texRef, data, desc.Width, desc.Height);
            _backend.ApplyTextureParams(texRef, desc.Preset, desc.Anisotropy, desc.LodBias);
        } 

        var hasMip = desc.Preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
        var levels = hasMip ? GfxUtils.CalcMipLevels(desc.Width, desc.Height) : 1;

        var meta = new TextureMeta(desc.Width, desc.Height, desc.Preset, desc.Kind, desc.Anisotropy, desc.Format,
            (byte)levels, false);

        return _resources.TextureStore.Add(in meta, texRef);
    }

    public TextureId CreateTexture2D(ReadOnlySpan<byte> data, in GpuTextureDescriptor desc)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)desc.Kind, (int)TextureKind.CubeMap, nameof(desc.Kind));

        var texRef = _backend.CreateTexture(in desc);
        if (desc.Kind != TextureKind.Multisample2D)
        {
            _backend.UploadTextureData(texRef, data, desc.Width, desc.Height);
            _backend.ApplyTextureParams(texRef, desc.Preset, desc.Anisotropy, desc.LodBias);
        } 

        var hasMip = desc.Preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
        var levels = hasMip ? GfxUtils.CalcMipLevels(desc.Width, desc.Height) : 1;

        var meta = new TextureMeta(desc.Width, desc.Height, desc.Preset, desc.Kind, desc.Anisotropy, desc.Format,
            (byte)levels, false);

        return _resources.TextureStore.Add(in meta, texRef);
    }

    public TextureId CreateCubeMap(in GpuTextureDescriptor desc)
    {
        if (desc.Kind == TextureKind.CubeMap)
            ArgumentOutOfRangeException.ThrowIfNotEqual(desc.Width, desc.Height, nameof(desc.Width));

        var texRef = _backend.CreateTexture(in desc, out var mipLevels);

        var meta = new TextureMeta(desc.Width, desc.Height, desc.Preset, desc.Kind, desc.Anisotropy, desc.Format,
            (byte)mipLevels, false);

        return _resources.TextureStore.Add(in meta, texRef);
    }

    public TextureId CreateTextureMsaa(in GpuTextureDescriptor desc, RenderBufferMsaa msaa)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(desc.Width, desc.Height, nameof(desc.Width));
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)msaa, (int)RenderBufferMsaa.None, nameof(msaa));

        var texRef = _backend.CreateTextureMsaa(in desc, samples);

        var meta = new TextureMeta(desc.Width, desc.Height, desc.Preset, desc.Kind, desc.Anisotropy, desc.Format, 0,
            false);

        return _resources.TextureStore.Add(in meta, texRef);
    }


    internal TextureId ReplaceTexture(TextureId textureId, ReadOnlySpan<byte> data,
        in GpuTextureDescriptor desc, out GfxRefToken<TextureId> newTexRef)
    {
        if (desc.Kind == TextureKind.CubeMap)
            ArgumentOutOfRangeException.ThrowIfNotEqual(desc.Width, desc.Height, nameof(desc.Width));

        newTexRef = _backend.CreateTexture(data, in desc, out var mipLevels);


        var meta = new TextureMeta(desc.Width, desc.Height, desc.Preset, desc.Kind, desc.Anisotropy, desc.Format,
            (byte)mipLevels, false);

        return _resources.TextureStore.Replace(textureId, in meta, newTexRef, out _);
    }


    public void UploadTextureData(TextureId textureId, ReadOnlySpan<byte> data, int width, int height)
    {
        var texture = _resources.TextureStore.GetHandleAndMeta(textureId, out var meta);
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Width, width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Height, height, nameof(height));

        _backend.UploadTextureData(in texture, data, width, height);
        var newMeta = TextureMeta.CreateFromHasData(in meta, true);
        _resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);
    }

    public void UploadCubeMapFace(TextureId textureId, ReadOnlySpan<byte> data, int width, int height, int faceIdx)
    {
        var texture = _resources.TextureStore.GetHandleAndMeta(textureId, out var meta);

        ArgumentOutOfRangeException.ThrowIfNotEqual(width, height, nameof(width));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(faceIdx, 5, nameof(faceIdx));
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Width, width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Height, height, nameof(height));

        _backend.UploadCubeMapFace(in texture, data, width, height, faceIdx);
        if (faceIdx == 5)
        {
            var newMeta = TextureMeta.CreateFromHasData(in meta, true);
            _resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);
        }
    }

    private sealed class GfxTexturesBackend
    {
        private readonly IGraphicsDriver _driver;
        private readonly GlTextures _drivTexture;

        internal GfxTexturesBackend(GfxContextInternal context)
        {
            _driver = context.Driver;
            _drivTexture = _driver.Textures;
        }

        public GfxRefToken<TextureId> CreateTexture(in GpuTextureDescriptor desc,
            RenderBufferMsaa msaa = RenderBufferMsaa.None)
        {
            if (desc.Kind == TextureKind.CubeMap)
            {
                return _drivTexture.CreateTextureCubeMap(desc.Width, desc.Height, msaa.ToSamples());
            }

            var texRef = desc.Kind switch
            {
                TextureKind.Texture2D => _driver.Textures.CreateTexture2D(desc.Width, desc.Height, mipLevels),
                TextureKind.Multisample2D => _driver.Textures.CreateTextureMultisample(desc.Width, desc.Height,
                    mipLevels),
                _ => throw new ArgumentOutOfRangeException()
            };
            return texRef;
        }

        public GfxRefToken<TextureId> CreateTexture2D(int width, int height, int levels)
            => _driver.Textures.CreateTexture2D(width, height, levels);

        public GfxRefToken<TextureId> CreateTextureCubeMap(int width, int height, int levels)
            => _driver.Textures.CreateTextureCubeMap(width, height, levels);

        public GfxRefToken<TextureId> CreateTextureMultisample(int width, int height, int samples)
            => _driver.Textures.CreateTextureMultisample(width, height, samples);

        public void ApplyTextureParams(GfxRefToken<TextureId> texRef, TexturePreset preset,
            TextureAnisotropy anisotropy, float lodBias)
        {
            _driver.Textures.SetTexturePreset(texRef, preset);

            if (anisotropy != TextureAnisotropy.Off)
                _driver.Textures.SetAnisotropy(texRef, anisotropy.ToAnisotropy());
            if (lodBias != 0)
                _driver.Textures.SetLodBias(texRef, lodBias);
        }

        public void GenerateMipMaps(GfxRefToken<TextureId> texRef) => _drivTexture.GenerateMipMaps(texRef);
        
/*
        public GfxRefToken<TextureId> CreateTexture2D(ReadOnlySpan<byte> data, in GpuTextureDescriptor desc,
            int mipLevels)
        {
            var hasMipLevels = desc.Preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
            mipLevels = hasMipLevels ? GfxUtils.CalcMipLevels(desc.Width, desc.Height) : 1;

            var texRef = desc.Kind switch
            {
                TextureKind.Texture2D => _driver.Textures.CreateTexture2D(desc.Width, desc.Height, mipLevels),
                TextureKind.CubeMap => _driver.Textures.CreateTexture2D(desc.Width, desc.Height, mipLevels),
                TextureKind.Multisample2D => _driver.Textures.CreateTexture2D(desc.Width, desc.Height, mipLevels),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (!data.IsEmpty)
                _driver.Textures.UploadTextureData(texRef.Handle, data, desc.Width, desc.Height);
            else
                _driver.Textures.UploadTextureEmptyData(texRef.Handle);

            _driver.Textures.SetTexturePreset(texRef.Handle, desc.Preset);

            if (desc.Anisotropy != TextureAnisotropy.Off)
                _driver.Textures.SetAnisotropy(texRef.Handle, desc.Anisotropy.ToAnisotropy());
            if (desc.LodBias != 0)
                _driver.Textures.SetLodBias(texRef.Handle, desc.LodBias);

            return texRef;
        }

        public GfxRefToken<TextureId> CreateCubeMap(in GpuTextureDescriptor desc, out int mipLevels)
        {
            var hasMipLevels = desc.Preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
            mipLevels = hasMipLevels ? GfxUtils.CalcMipLevels(desc.Width, desc.Height) : 1;

            var texRef = _driver.Textures.CreateTextureCubeMap(desc.Width, desc.Height, mipLevels);
            _driver.Textures.SetTexturePreset(texRef.Handle, desc.Preset);

            if (desc.Anisotropy != TextureAnisotropy.Off)
                _driver.Textures.SetAnisotropy(texRef.Handle, desc.Anisotropy.ToAnisotropy());
            if (desc.LodBias != 0)
                _driver.Textures.SetLodBias(texRef.Handle, desc.LodBias);

            return texRef;
        }

        public GfxRefToken<TextureId> CreateTextureMsaa(in GpuTextureDescriptor desc, int samples)
        {
            var texRef = _driver.Textures.CreateTextureMultisample(desc.Width, desc.Height, samples);
            return texRef;
        }*/

        public void UploadTextureData(GfxRefToken<TextureId> texRef, ReadOnlySpan<byte> data, int width, int height)
            => _driver.Textures.UploadTextureData(texRef, data, width, height);

        public void UploadTextureEmptyData(GfxRefToken<TextureId> texRef)
            => _driver.Textures.UploadTextureEmptyData(texRef);


        public void UploadCubeMapFace(GfxRefToken<TextureId> texRef, ReadOnlySpan<byte> data, int width, int height,
            int faceIdx)
        {
            _driver.Textures.UploadCubeMapFaceData(texRef, data, width, height, faceIdx);
        }
    }
}