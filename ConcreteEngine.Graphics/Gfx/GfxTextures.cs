using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxTextures
{
    private readonly FrontendStoreHub _resources;
    private readonly GfxResourceRepository _repository;
    private readonly GfxTexturesBackend _backend;


    internal GfxTextures(GfxContext context)
    {
        _backend = new GfxTexturesBackend(context);
        _resources = context.Stores;
        _repository = context.Repositories;
    }

    public TextureId CreateTexture(ReadOnlySpan<byte> data, in GpuTextureDescriptor desc)
    {
        if (desc.Kind == TextureKind.CubeMap)
            ArgumentOutOfRangeException.ThrowIfNotEqual(desc.Width, desc.Height, nameof(desc.Width));

        var texRef = _backend.CreateTexture(data, in desc, out var mipLevels);

        var meta = new TextureMeta(desc.Width, desc.Height, desc.Preset, desc.Kind, desc.Anisotropy, desc.Format,
            (byte)mipLevels, false);

        return _resources.TextureStore.Add(in meta, texRef);
    }

    public TextureId ReplaceTexture(TextureId textureId, ReadOnlySpan<byte> data, 
        in GpuTextureDescriptor desc, out  GfxRefToken<TextureId> newTexRef)
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

        internal GfxTexturesBackend(GfxContext context)
        {
            _driver = context.Driver;
        }

        public GfxRefToken<TextureId> CreateTexture(ReadOnlySpan<byte> data, in GpuTextureDescriptor desc,
            out int mipLevels)
        {
            var hasMipLevels = desc.Preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
            mipLevels = hasMipLevels ? GfxUtils.CalcMipLevels(desc.Width, desc.Height) : 0;

            var texRef = _driver.Textures.CreateTexture2D((uint)desc.Width, (uint)desc.Height, (uint)mipLevels);

            if (!data.IsEmpty) 
                _driver.Textures.UploadTextureData(texRef.Handle, data, (uint)desc.Width, (uint)desc.Height);
            else 
                _driver.Textures.UploadTextureEmptyData(texRef.Handle);

            _driver.Textures.SetTexturePreset(texRef.Handle, desc.Preset);

            if (desc.Anisotropy != TextureAnisotropy.Off)
                _driver.Textures.SetAnisotropy(texRef.Handle, desc.Anisotropy.ToAnisotropy());
            if (desc.LodBias != 0)
                _driver.Textures.SetLodBias(texRef.Handle, desc.LodBias);

            return texRef;
        }


        public void UploadTextureData(in GfxHandle texture, ReadOnlySpan<byte> data, int width, int height)
        {
            _driver.Textures.UploadTextureData(texture, data, (uint)width, (uint)height);
        }

        public void UploadCubeMapFace(in GfxHandle texture, ReadOnlySpan<byte> data, int width, int height,
            int faceIdx)
        {
            _driver.Textures.UploadCubeMapFaceData(texture, data, (uint)width, (uint)height, faceIdx);
        }
    }
}