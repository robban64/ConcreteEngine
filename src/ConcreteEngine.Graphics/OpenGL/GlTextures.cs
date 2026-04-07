using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.OpenGL.Utilities;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlTextures : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendResourceStore<GlHandle> _textureStore;


    internal GlTextures(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _textureStore = ctx.Store.TextureStore;
    }


    public GfxHandle CreateTexture(TextureKind kind)
    {
        _gl.CreateTextures(kind.ToGlEnum(), 1, out uint texture);
        return _textureStore.Add(new GlHandle(texture));
    }


    public void TextureStorage2D(GfxHandle texRef, Size2D size, GpuTextureProps desc)
    {
        var handle = _textureStore.GetHandle(texRef);
        (uint width, uint height) = size.ToUnsigned();
        _gl.TextureStorage2D(handle, desc.Levels, desc.Format.ToStorageFormat(), width, height);
    }

    public void TextureStorage2D_MultiSample(GfxHandle texRef, Size2D size, GpuTextureProps desc)
    {
        var handle = _textureStore.GetHandle(texRef);
        (uint width, uint height) = size.ToUnsigned();
        _gl.TextureStorage2DMultisample(handle, desc.Samples, desc.Format.ToStorageFormat(), width, height, true);
    }

    public void TextureStorage3D(GfxHandle texRef, Size3D size, GpuTextureProps desc)
    {
        var handle = _textureStore.GetHandle(texRef);
        (uint width, uint height, uint depth) = size.ToUnsigned();
        _gl.TextureStorage3D(handle, desc.Levels, desc.Format.ToStorageFormat(), width, height, depth);
    }

    public void UploadTexture2D_Data(GfxHandle texRef, ReadOnlySpan<byte> data, TexturePixelFormat format,
        Size2D size)
    {
        var handle = _textureStore.GetHandle(texRef);
        (uint width, uint height) = size.ToUnsigned();
        var (fmt, type) = format.ToUploadFormatType();
        _gl.TextureSubImage2D(handle, 0, 0, 0, width, height, fmt, type, data);
    }

    public void UploadTexture3D_Data(GfxHandle texRef, ReadOnlySpan<byte> data, TexturePixelFormat format,
        Size3D size, int zOffset)
    {
        var handle = _textureStore.GetHandle(texRef);
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


    public void SetLodBias(GfxHandle texRef, float lodBias) =>
        _gl.TextureParameter(_textureStore.GetHandle(texRef), GLEnum.TextureLodBias, lodBias);

    public void SetAnisotropy(GfxHandle texRef, int anisotropy)
    {
        var handle = _textureStore.GetHandle(texRef);
        _gl.TextureParameter(handle.Value, GLEnum.TextureMaxAnisotropy, anisotropy);
    }

    public void GenerateMipMaps(GfxHandle texRef) =>
        _gl.GenerateTextureMipmap(_textureStore.GetHandle(texRef));

    public void SetBorder(GfxHandle texRef, GpuTextureBorder b)
    {
        var handle = _textureStore.GetHandle(texRef);
        Span<int> border = stackalloc int[] { b.R, b.G, b.B, b.A };
        _gl.TextureParameterI(handle.Value, GLEnum.TextureBorderColor, border);
    }

    public void SetCompareTextureFunc(GfxHandle texRef, DepthMode depthMode)
    {
        if (depthMode == DepthMode.Unset) return;

        var handle = _textureStore.GetHandle(texRef);
        var depthFunc = depthMode.ToGlEnum();
        _gl.TextureParameterI(handle.Value, GLEnum.TextureCompareMode, (int)GLEnum.CompareRefToTexture);
        _gl.TextureParameterI(handle.Value, GLEnum.TextureCompareFunc, (int)depthFunc);
    }

    public void SetTexturePreset(GfxHandle texRef, TexturePreset preset, bool wrapR)
    {
        var handle = _textureStore.GetHandle(texRef);

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