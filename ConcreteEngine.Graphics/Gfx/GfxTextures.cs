using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxTextures
{
    private readonly FrontendStoreHub _resources;
    private readonly GfxResourceRepository _repository;
    
    private readonly GfxTextureInvoker _invoker;

    internal GfxTextures(GfxContext context)
    {
        _invoker = new GfxTextureInvoker(context);
        _resources = context.Stores;
        _repository = context.Repositories;
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

    public void UploadTextureData(in TextureId textureId, ReadOnlySpan<byte> data, uint width, uint height)
    {
        var texture = _resources.TextureStore.GetHandleAndMeta(textureId, out var meta);
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Width, width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Height, height, nameof(height));

        _invoker.UploadTextureData(in texture, data, width, height);
        var newMeta = TextureMeta.CreateFromHasData(in meta, true);
        _resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);
    }

    public void UploadCubeMapFace(in TextureId textureId, ReadOnlySpan<byte> data, uint width, uint height, int faceIdx)
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