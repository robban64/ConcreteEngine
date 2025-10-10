#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.OpenGL.Utilities;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlTextures : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendOpsHub _store;
    private readonly GlCapabilities _capabilities;


    internal GlTextures(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _capabilities = ctx.Capabilities;
        _store = ctx.Store;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GlTextureHandle GetTexHandle(GfxRefToken<TextureId> handle) => _store.Texture.GetRef(handle);

    public GfxRefToken<TextureId> CreateTexture(TextureKind kind)
    {
        _gl.CreateTextures(kind.ToGlEnum(), 1, out uint texture);
        return _store.Texture.Add(new GlTextureHandle(texture));
    }

    public void TextureStorage2D(GfxRefToken<TextureId> texRef, Size2D size, BkTextureStoreDesc desc)
    {
        var handle = GetTexHandle(texRef).Value;
        (uint width, uint height) = size.ToUnsigned();
        _gl.TextureStorage2D(handle, desc.Levels, desc.Format.ToStorageFormat(), width, height);
    }

    public void TextureStorage2D_MultiSample(GfxRefToken<TextureId> texRef, Size2D size, BkTextureStoreDesc desc)
    {
        var handle = GetTexHandle(texRef).Value;
        (uint width, uint height) = size.ToUnsigned();
        _gl.TextureStorage2DMultisample(handle, desc.Samples, desc.Format.ToStorageFormat(), width, height, true);
    }

    public void TextureStorage3D(GfxRefToken<TextureId> texRef, Size3D size, BkTextureStoreDesc desc)
    {
        var handle = GetTexHandle(texRef).Value;
        (uint width, uint height, uint depth) = size.ToUnsigned();
        _gl.TextureStorage3D(handle, desc.Levels, desc.Format.ToStorageFormat(), width, height, depth);
    }

    public void UploadTexture2D_Data(GfxRefToken<TextureId> texRef, ReadOnlySpan<byte> data, GfxPixelFormat format,
        Size2D size)
    {
        var handle = GetTexHandle(texRef).Value;
        (uint width, uint height) = size.ToUnsigned();
        var (fmt, type) = format.ToUploadFormatType();
        _gl.TextureSubImage2D(handle, 0, 0, 0, width, height, fmt, type, data);
    }

    public void UploadTexture3D_Data(GfxRefToken<TextureId> texRef, ReadOnlySpan<byte> data, GfxPixelFormat format,
        Size3D size, int zOffset)
    {
        var handle = GetTexHandle(texRef).Value;
        (uint width, uint height, uint depth) = size.ToUnsigned();
        var (fmt, type) = format.ToUploadFormatType();
        _gl.TextureSubImage3D(
            handle, level: 0,
            xoffset: 0, yoffset: 0, zoffset: zOffset,
            width: width, height: height, depth: depth,
            format: fmt, type: type,
            pixels: data
        );
    }


    public void SetLodBias(GfxRefToken<TextureId> texRef, float lodBias) =>
        _gl.TextureParameter(GetTexHandle(texRef).Value, GLEnum.TextureLodBias, lodBias);

    public void SetAnisotropy(GfxRefToken<TextureId> texRef, int anisotropy) =>
        _gl.TextureParameter(GetTexHandle(texRef).Value, GLEnum.TextureMaxAnisotropy, anisotropy);

    public void GenerateMipMaps(GfxRefToken<TextureId> texRef) =>
        _gl.GenerateTextureMipmap(GetTexHandle(texRef).Value);

    public void ApplyCompareAndBorder(GfxRefToken<TextureId> texRef)
    {
        var handle = GetTexHandle(texRef);

        Span<int> border = stackalloc int[] { 1, 1, 1, 1 };
        _gl.TextureParameterI(handle.Value, GLEnum.TextureCompareMode, (int)GLEnum.CompareRefToTexture);
        _gl.TextureParameterI(handle.Value, GLEnum.TextureCompareFunc, (int)GLEnum.Lequal);
        _gl.TextureParameterI(handle.Value, GLEnum.TextureBorderColor, border);
    }

    public void SetTexturePreset(GfxRefToken<TextureId> texRef, TexturePreset preset, bool wrapR)
    {
        var handle = GetTexHandle(texRef);

        switch (preset)
        {
            case TexturePreset.NearestClamp:
            case TexturePreset.NearestClampBorder:
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.Nearest);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Nearest);
                var nparam = preset == TexturePreset.NearestClamp ? GLEnum.ClampToEdge : GLEnum.ClampToBorder;
                SetTexParameter(GLEnum.TextureWrapS, nparam);
                SetTexParameter(GLEnum.TextureWrapT, nparam);
                if (wrapR) SetTexParameter(GLEnum.TextureWrapR, nparam);
                break;

            case TexturePreset.NearestRepeat:
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.Nearest);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Nearest);
                SetTexParameter(GLEnum.TextureWrapS, GLEnum.Repeat);
                SetTexParameter(GLEnum.TextureWrapT, GLEnum.Repeat);
                if (wrapR) SetTexParameter(GLEnum.TextureWrapR, GLEnum.Repeat);
                break;

            case TexturePreset.LinearClamp:
            case TexturePreset.LinearClampBorder:
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.Linear);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Linear);
                var param = preset == TexturePreset.LinearClamp ? GLEnum.ClampToEdge : GLEnum.ClampToBorder;
                SetTexParameter(GLEnum.TextureWrapS, param);
                SetTexParameter(GLEnum.TextureWrapT, param);
                if (wrapR) SetTexParameter(GLEnum.TextureWrapR, param);
                break;

            case TexturePreset.LinearRepeat:
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.Linear);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Linear);
                SetTexParameter(GLEnum.TextureWrapS, GLEnum.Repeat);
                SetTexParameter(GLEnum.TextureWrapT, GLEnum.Repeat);
                if (wrapR) SetTexParameter(GLEnum.TextureWrapR, GLEnum.Repeat);
                break;

            case TexturePreset.LinearMipmapClamp:
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.LinearMipmapLinear);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Linear);
                SetTexParameter(GLEnum.TextureWrapS, GLEnum.ClampToEdge);
                SetTexParameter(GLEnum.TextureWrapT, GLEnum.ClampToEdge);
                if (wrapR) SetTexParameter(GLEnum.TextureWrapR, GLEnum.ClampToEdge);
                break;

            case TexturePreset.LinearMipmapRepeat:
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.LinearMipmapLinear);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Linear);
                SetTexParameter(GLEnum.TextureWrapS, GLEnum.Repeat);
                SetTexParameter(GLEnum.TextureWrapT, GLEnum.Repeat);
                if (wrapR) SetTexParameter(GLEnum.TextureWrapR, GLEnum.Repeat);

                break;

            case TexturePreset.PremultipliedUi:
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.Linear);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Linear);
                SetTexParameter(GLEnum.TextureWrapS, GLEnum.ClampToEdge);
                SetTexParameter(GLEnum.TextureWrapT, GLEnum.ClampToEdge);
                if (wrapR) SetTexParameter(GLEnum.TextureWrapR, GLEnum.ClampToEdge);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
        }

        return;
        void SetTexParameter(GLEnum pname, GLEnum param) => _gl.TextureParameterI(handle.Value, pname, (int)param);
    }
}