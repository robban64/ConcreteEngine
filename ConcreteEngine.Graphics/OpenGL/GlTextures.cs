#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlTextures : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendOpsHub _store;
    private readonly GlCapabilities _capabilities;

    private SizedInternalFormat ColorFormat => SizedInternalFormat.Rgba8;

    internal GlTextures(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _capabilities = ctx.Capabilities;
        _store = ctx.Store;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GlTextureHandle GetTexHandle(in GfxRefToken<TextureId> handle) => _store.Texture.GetRef(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindTexture(in GfxRefToken<TextureId> handle, int slot) =>
        _gl.BindTextureUnit((uint)slot, GetTexHandle(in handle).Handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindTextureSlot(int slot) => _gl.BindTextureUnit(0, (uint)slot);

    public GfxRefToken<TextureId> CreateTexture2D(int width, int height, int mipLevels)
    {
        _gl.CreateTextures(TextureTarget.Texture2D, 1, out uint texture);
        _gl.TextureStorage2D(texture, (uint)mipLevels, ColorFormat, (uint)width, (uint)height);
        return _store.Texture.Add(new GlTextureHandle(texture));
    }

    public GfxRefToken<TextureId> CreateTextureCubeMap(int width, int height, int mipLevels)
    {
        _gl.CreateTextures(TextureTarget.TextureCubeMap, 1, out uint texture);
        _gl.TextureStorage2D(texture, (uint)mipLevels, ColorFormat, (uint)width, (uint)height);
        return _store.Texture.Add(new GlTextureHandle(texture));
    }

    public GfxRefToken<TextureId> CreateTextureMultisample(int width, int height, int samples)
    {
        _gl.CreateTextures(TextureTarget.Texture2DMultisample, 1, out uint texture);
        _gl.TextureStorage2DMultisample(texture, (uint)samples, SizedInternalFormat.Srgb8Alpha8, (uint)width,
            (uint)height, true);
        return _store.Texture.Add(new GlTextureHandle(texture));
    }

    private void CreateTextureStore(GfxRefToken<TextureId> texRef, int width, int height, int mipLevels)
    {
        var handle = GetTexHandle(in texRef).Handle;
        var levels = Math.Min(1, mipLevels);
        _gl.TextureStorage2D(handle, (uint)levels, ColorFormat, (uint)width, (uint)height);
    }

    public void UploadTextureData(GfxRefToken<TextureId> texRef, ReadOnlySpan<byte> data, int width, int height)
    {
        var handle = GetTexHandle(in texRef).Handle;
        _gl.TextureSubImage2D(handle, 0, 0, 0, (uint)width, (uint)height,
            PixelFormat.Rgba, PixelType.UnsignedByte, data);
    }

    public unsafe void UploadTextureEmptyData(GfxRefToken<TextureId> texRef)
    {
        var handle = GetTexHandle(in texRef).Handle;
        _gl.TextureSubImage2D(handle, 0, 0, 0, 1, 1,
            PixelFormat.Rgba, PixelType.UnsignedByte, (void*)0);
    }

    public void UploadCubeMapFaceData(GfxRefToken<TextureId> texRef, ReadOnlySpan<byte> data, int width, int height,
        int faceIdx)
    {
        var handle = GetTexHandle(in texRef).Handle;
        _gl.TextureSubImage3D(
            handle, level: 0,
            xoffset: 0, yoffset: 0, zoffset: faceIdx,
            width: (uint)width, height: (uint)height, depth: 1,
            format: PixelFormat.Rgba, type: PixelType.UnsignedByte,
            pixels: data
        );
    }

    public void SetTexturePreset(GfxRefToken<TextureId> texRef, TexturePreset preset)
    {
        var handle = GetTexHandle(in texRef);

        switch (preset)
        {
            case TexturePreset.NearestClamp:
                SetTexParameter(GLEnum.TextureWrapS, GLEnum.ClampToEdge);
                SetTexParameter(GLEnum.TextureWrapT, GLEnum.ClampToEdge);
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.Nearest);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Nearest);
                break;

            case TexturePreset.NearestRepeat:
                SetTexParameter(GLEnum.TextureWrapS, GLEnum.Repeat);
                SetTexParameter(GLEnum.TextureWrapT, GLEnum.Repeat);
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.Nearest);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Nearest);
                break;

            case TexturePreset.LinearClamp:
                SetTexParameter(GLEnum.TextureWrapS, GLEnum.ClampToEdge);
                SetTexParameter(GLEnum.TextureWrapT, GLEnum.ClampToEdge);
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.Linear);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Linear);
                break;

            case TexturePreset.LinearRepeat:
                SetTexParameter(GLEnum.TextureWrapS, GLEnum.Repeat);
                SetTexParameter(GLEnum.TextureWrapT, GLEnum.Repeat);
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.Linear);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Linear);
                break;

            case TexturePreset.LinearMipmapClamp:
                SetTexParameter(GLEnum.TextureWrapS, GLEnum.ClampToEdge);
                SetTexParameter(GLEnum.TextureWrapT, GLEnum.ClampToEdge);
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.LinearMipmapLinear);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Linear);
                break;

            case TexturePreset.LinearMipmapRepeat:
                SetTexParameter(GLEnum.TextureWrapS, GLEnum.Repeat);
                SetTexParameter(GLEnum.TextureWrapT, GLEnum.Repeat);
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.LinearMipmapLinear);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Linear);
                break;

            case TexturePreset.PremultipliedUi:
                SetTexParameter(GLEnum.TextureWrapS, GLEnum.ClampToEdge);
                SetTexParameter(GLEnum.TextureWrapT, GLEnum.ClampToEdge);
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.Linear);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Linear);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
        }

        return;
        void SetTexParameter(GLEnum pname, GLEnum param) => _gl.TextureParameterI(handle.Handle, pname, (int)param);
    }

    public void SetLodBias(GfxRefToken<TextureId> texRef, float lodBias) =>
        _gl.TextureParameter(GetTexHandle(in texRef).Handle, GLEnum.TextureLodBias, lodBias);

    public void SetAnisotropy(GfxRefToken<TextureId> texRef, int anisotropy) =>
        _gl.TextureParameter(GetTexHandle(in texRef).Handle, GLEnum.TextureLodBias, anisotropy);

    public void GenerateMipMaps(GfxRefToken<TextureId> texRef) =>
        _gl.GenerateTextureMipmap(GetTexHandle(in texRef).Handle);
}