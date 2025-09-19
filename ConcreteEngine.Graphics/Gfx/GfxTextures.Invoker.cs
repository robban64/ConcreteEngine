using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxTexturesInvoker
{
    private readonly IGraphicsDriver _driver;

    internal GfxTexturesInvoker(GfxContext context)
    {
        _driver = context.Driver;
    }
    
    public GfxRefToken<TextureId> CreateTexture(ReadOnlySpan<byte> data, in GpuTextureDescriptor desc,
        out uint mipLevels)
    {
        var hasMipLevels = desc.Preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
        mipLevels = hasMipLevels ? GfxUtils.CalcMipLevels(desc.Width, desc.Height) : 0;

        var texRef = _driver.Textures.CreateTexture2D(desc.Width, desc.Height, mipLevels);

        if (!data.IsEmpty) _driver.Textures.UploadTextureData(texRef.Handle, data, desc.Width, desc.Height);
        else _driver.Textures.UploadTextureEmptyData(texRef.Handle);

        _driver.Textures.SetTexturePreset(texRef.Handle, desc.Preset);

        if (desc.Anisotropy != TextureAnisotropy.Off)
            _driver.Textures.SetAnisotropy(texRef.Handle, desc.Anisotropy.ToAnisotropy());
        if (desc.LodBias != 0)
            _driver.Textures.SetLodBias(texRef.Handle, desc.LodBias);

        return texRef;
    }
    
    
    public void UploadTextureData(in GfxHandle texture, ReadOnlySpan<byte> data, uint width, uint height)
    {
        _driver.Textures.UploadTextureData(texture, data, width, height);
    }

    public void UploadCubeMapFace(in GfxHandle texture, ReadOnlySpan<byte> data, uint width, uint height, int faceIdx)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(width, height, nameof(width));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(faceIdx, 5, nameof(faceIdx));

        _driver.Textures.UploadCubeMapFaceData(texture, data, width, height, faceIdx);
        if (faceIdx == 5)
        {
            //var newMeta = TextureMeta.CreateFromHasData(in meta, true);
            //_resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);
        }
    }
}