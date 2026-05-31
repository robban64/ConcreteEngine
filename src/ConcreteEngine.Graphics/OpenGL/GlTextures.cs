using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlTextures
{
    private static GL Gl => GlBackendDriver.Gl;
    private readonly BackendResourceStore _textureStore = GfxRegistry.GetBackendStore<TextureMeta>();


    public GfxHandle CreateTexture(TextureKind kind)
    {
        Gl.CreateTextures(kind.ToGlEnum(), 1, out uint texture);
        return _textureStore.Add(new NativeHandle(texture));
    }


    public void TextureStorage2D(GfxHandle texRef, Size2D size, GpuTextureProps desc)
    {
        var handle = _textureStore.Get(texRef);
        (uint width, uint height) = size.ToUnsigned();
        Gl.TextureStorage2D(handle, desc.Levels, desc.Format.ToStorageFormat(), width, height);
    }

    public void TextureStorage2D_MultiSample(GfxHandle texRef, Size2D size, GpuTextureProps desc)
    {
        var handle = _textureStore.Get(texRef);
        (uint width, uint height) = size.ToUnsigned();
        Gl.TextureStorage2DMultisample(handle, desc.Samples, desc.Format.ToStorageFormat(), width, height, true);
    }

    public void TextureStorage3D(GfxHandle texRef, Size3D size, GpuTextureProps desc)
    {
        var handle = _textureStore.Get(texRef);
        (uint width, uint height, uint depth) = size.ToUnsigned();
        Gl.TextureStorage3D(handle, desc.Levels, desc.Format.ToStorageFormat(), width, height, depth);
    }

    public void UploadTexture2D_Data(GfxHandle texRef, ReadOnlySpan<byte> data, TexturePixelFormat format,
        Size2D size)
    {
        var handle = _textureStore.Get(texRef);
        (uint width, uint height) = size.ToUnsigned();
        var (fmt, type) = format.ToUploadFormatType();
        Gl.TextureSubImage2D(handle, 0, 0, 0, width, height, fmt, type, data);
    }

    public void UploadTexture3D_Data(GfxHandle texRef, ReadOnlySpan<byte> data, TexturePixelFormat format,
        Size3D size, int zOffset)
    {
        var handle = _textureStore.Get(texRef);
        (uint width, uint height, uint depth) = size.ToUnsigned();
        var (fmt, type) = format.ToUploadFormatType();
        Gl.TextureSubImage3D(
            handle, level: 0,
            xoffset: 0, yoffset: 0, zoffset: zOffset,
            width: width, height: height, depth: depth,
            format: fmt, type: type,
            pixels: data
        );
    }

    public void CopyTextureData(
        GfxHandle src, TextureKind srcKind, GfxHandle dst, TextureKind dstKind,
        int srcLevel, int dstLevel, Size3D srcSize,
        Vector3I srcPos = default, Vector3I dstPos = default)
    {
        var srcHandle = _textureStore.Get(src);
        var dstHandle = _textureStore.Get(dst);
        (uint width, uint height, uint depth) = srcSize.ToUnsigned();

        Gl.CopyImageSubData(
            srcHandle, srcKind.ToGlEnum(), srcLevel, srcPos.X, srcPos.Y, srcPos.Z,
            dstHandle, dstKind.ToGlEnum(), dstLevel, dstPos.X, dstPos.Y, dstPos.Z,
            width, height, depth
        );
    }


    public void SetLodBias(GfxHandle texRef, float lodBias) =>
        Gl.TextureParameter(_textureStore.Get(texRef), GLEnum.TextureLodBias, lodBias);

    public void SetAnisotropy(GfxHandle texRef, int anisotropy)
    {
        var handle = _textureStore.Get(texRef);
        Gl.TextureParameter(handle, GLEnum.TextureMaxAnisotropy, anisotropy);
    }

    public void GenerateMipMaps(GfxHandle texRef) => Gl.GenerateTextureMipmap(_textureStore.Get(texRef));

    public void SetBorder(GfxHandle texRef, GpuTextureBorder b)
    {
        var handle = _textureStore.Get(texRef);
        Span<int> border = stackalloc int[] { b.R, b.G, b.B, b.A };
        Gl.TextureParameterI(handle, GLEnum.TextureBorderColor, border);
    }

    public void SetCompareTextureFunc(GfxHandle texRef, DepthMode depthMode)
    {
        if (depthMode == DepthMode.Unset) return;

        var handle = _textureStore.Get(texRef);
        var compareMode = (int)GLEnum.CompareRefToTexture;
        var depthFunc = (int)depthMode.ToGlEnum();
        Gl.TextureParameterI(handle, GLEnum.TextureCompareMode, in compareMode);
        Gl.TextureParameterI(handle, GLEnum.TextureCompareFunc, in depthFunc);
    }

    public void SetTexturePreset(GfxHandle texRef, TexturePreset preset, bool wrapR)
    {
        var handle = _textureStore.Get(texRef);

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

        void SetTexParameter(GLEnum pName, GLEnum param)
        {
            var intParam = (int)param;
            Gl.TextureParameterI(handle, pName, ref intParam);
        }
    }
}