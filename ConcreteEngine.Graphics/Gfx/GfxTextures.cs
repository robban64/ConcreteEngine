using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxTextures
{
    private readonly FrontendStoreHub _resources;
    private readonly GfxResourceRepository _repository;

    private readonly GfxTexturesInvoker _invoker;

    public GfxTexturesInvoker Invoker => _invoker;

    internal GfxTextures(GfxContext context)
    {
        _invoker = new GfxTexturesInvoker(context);
        _resources = context.Stores;
        _repository = context.Repositories;
    }

    internal TextureId AddTextureInternal(GfxRefToken<TextureId> texRef, in TextureMeta meta)
    {
        return _resources.TextureStore.Add(in meta, texRef);
    }

    internal TextureId ReplaceInternal(TextureId textureId, GfxRefToken<TextureId> texRef, in TextureMeta meta)
    {
        _resources.TextureStore.Replace(textureId, in meta, texRef, out _);
        return _resources.TextureStore.Add(in meta, texRef);
    }


    public TextureId CreateTexture(ReadOnlySpan<byte> data, in GpuTextureDescriptor desc)
    {
        if (desc.Kind == TextureKind.CubeMap)
            ArgumentOutOfRangeException.ThrowIfNotEqual(desc.Width, desc.Height, nameof(desc.Width));

        var texRef = _invoker.CreateTexture(data, in desc, out uint mipLevels);

        var meta = new TextureMeta(desc.Width, desc.Height, desc.Preset, desc.Kind, desc.Anisotropy, desc.Format,
            (byte)mipLevels, false);

        return _resources.TextureStore.Add(in meta, texRef);
    }
    
    public TextureId ReplaceTexture(TextureId textureId, ReadOnlySpan<byte> data, in GpuTextureDescriptor desc)
    {
        if (desc.Kind == TextureKind.CubeMap)
            ArgumentOutOfRangeException.ThrowIfNotEqual(desc.Width, desc.Height, nameof(desc.Width));

        var texRef = _invoker.CreateTexture(data, in desc, out uint mipLevels);

        var meta = new TextureMeta(desc.Width, desc.Height, desc.Preset, desc.Kind, desc.Anisotropy, desc.Format,
            (byte)mipLevels, false);

        return _resources.TextureStore.Replace(textureId,in meta, texRef, out _);
    }


    public void UploadTextureData(TextureId textureId, ReadOnlySpan<byte> data, uint width, uint height)
    {
        var texture = _resources.TextureStore.GetHandleAndMeta(textureId, out var meta);
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Width, width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Height, height, nameof(height));

        _invoker.UploadTextureData(in texture, data, width, height);
        var newMeta = TextureMeta.CreateFromHasData(in meta, true);
        _resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);
    }

    public void UploadCubeMapFace(TextureId textureId, ReadOnlySpan<byte> data, uint width, uint height, int faceIdx)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(width, height, nameof(width));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(faceIdx, 5, nameof(faceIdx));

        var texture = _resources.TextureStore.GetHandleAndMeta(textureId, out var meta);

        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Width, width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Height, height, nameof(height));

        _invoker.UploadCubeMapFace(in texture, data, width, height, faceIdx);
        if (faceIdx == 5)
        {
            var newMeta = TextureMeta.CreateFromHasData(in meta, true);
            _resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);
        }
    }
}