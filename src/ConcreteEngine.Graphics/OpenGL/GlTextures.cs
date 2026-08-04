using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using Silk.NET.OpenGL;
using static ConcreteEngine.Graphics.OpenGL.GlDriver;

namespace ConcreteEngine.Graphics.OpenGL;

internal static class GlTextures
{
    public static NativeHandle CreateTexture(TextureKind kind)
    {
        Gl.CreateTextures(kind.ToGlEnum(), 1, out uint texture);
        return new NativeHandle(texture);
    }

    public static NativeHandle CreateSampler()
    {
        Gl.CreateSamplers(1, out uint samplerId);
        return new NativeHandle(samplerId);
    }

    public static void TextureStorage2D(NativeHandle handle, Size2D size, GpuTextureProps desc)
    {
        (uint width, uint height) = size.ToUnsigned();
        Gl.TextureStorage2D(handle, desc.Levels, desc.Format.ToStorageFormat(), width, height);
    }

    public static void TextureStorage2D_MultiSample(NativeHandle handle, Size2D size, GpuTextureProps desc)
    {
        (uint width, uint height) = size.ToUnsigned();
        Gl.TextureStorage2DMultisample(handle, desc.Samples, desc.Format.ToStorageFormat(), width, height, true);
    }

    public static void TextureStorage3D(NativeHandle handle, Size3D size, GpuTextureProps desc)
    {
        (uint width, uint height, uint depth) = size.ToUnsigned();
        Gl.TextureStorage3D(handle, desc.Levels, desc.Format.ToStorageFormat(), width, height, depth);
    }

    public static void UploadTexture2D_Data(NativeHandle handle, ReadOnlySpan<byte> data, TexturePixelFormat format,
        Size2D size)
    {
        (uint width, uint height) = size.ToUnsigned();
        var (fmt, type) = format.ToUploadFormatType();
        Gl.TextureSubImage2D(handle, 0, 0, 0, width, height, fmt, type, data);
    }

    public static void UploadTexture3D_Data(NativeHandle handle, ReadOnlySpan<byte> data, TexturePixelFormat format,
        Size3D size, int zOffset)
    {
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

    public static void CopyTextureData(
        NativeHandle src, TextureKind srcKind, NativeHandle dst, TextureKind dstKind,
        int srcLevel, int dstLevel, Size3D srcSize,
        Int3 srcPos = default, Int3 dstPos = default)
    {
        (uint width, uint height, uint depth) = srcSize.ToUnsigned();

        Gl.CopyImageSubData(
            src, srcKind.ToGlEnum(), srcLevel, srcPos.X, srcPos.Y, srcPos.Z,
            dst, dstKind.ToGlEnum(), dstLevel, dstPos.X, dstPos.Y, dstPos.Z,
            width, height, depth
        );
    }


    public static void SetLodBias(NativeHandle handle, float lodBias) =>
        Gl.TextureParameter(handle, GLEnum.TextureLodBias, lodBias);

    public static void SetAnisotropy(NativeHandle handle, int anisotropy)
    {
        Gl.TextureParameter(handle, GLEnum.TextureMaxAnisotropy, anisotropy);
    }

    public static void SetSamplerLodBias(NativeHandle handle, float lodBias) =>
        Gl.SamplerParameter(handle, GLEnum.TextureLodBias, lodBias);

    public static void SetSamplerAnisotropy(NativeHandle handle, int anisotropy)
    {
        Gl.SamplerParameter(handle, GLEnum.TextureMaxAnisotropy, anisotropy);
    }

    public static void GenerateMipMaps(NativeHandle handle) => Gl.GenerateTextureMipmap(handle);

    public static void SetBorder(NativeHandle handle, TextureBorder b)
    {
        Span<int> border = stackalloc int[] { b.R, b.G, b.B, b.A };
        Gl.TextureParameterI(handle, GLEnum.TextureBorderColor, border);
    }

    public static void SetSamplerBorder(NativeHandle handle, TextureBorder b)
    {
        Span<int> border = stackalloc int[] { b.R, b.G, b.B, b.A };
        Gl.SamplerParameterI(handle, GLEnum.TextureBorderColor, border);
    }

    public static void SetCompareTextureFunc(NativeHandle handle, DepthMode depthMode)
    {
        if (depthMode == DepthMode.Unset) return;

        var compareMode = (int)GLEnum.CompareRefToTexture;
        var depthFunc = (int)depthMode.ToGlEnum();
        Gl.TextureParameterI(handle, GLEnum.TextureCompareMode, in compareMode);
        Gl.TextureParameterI(handle, GLEnum.TextureCompareFunc, in depthFunc);
    }

    public static void SetSamplerCompareTextureFunc(NativeHandle handle, DepthMode depthMode)
    {
        if (depthMode == DepthMode.Unset) return;

        var compareMode = (int)GLEnum.CompareRefToTexture;
        var depthFunc = (int)depthMode.ToGlEnum();
        Gl.SamplerParameterI(handle, GLEnum.TextureCompareMode, in compareMode);
        Gl.SamplerParameterI(handle, GLEnum.TextureCompareFunc, in depthFunc);
    }

    public static void SetTexturePreset(NativeHandle handle, TexturePreset preset, bool wrapR)
    {
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

    public static void SetSamplerPreset(NativeHandle samplerHandle, TexturePreset preset, bool wrapR)
    {
        switch (preset)
        {
            case TexturePreset.NearestClamp:
            case TexturePreset.NearestClampBorder:
                SetParameter(samplerHandle, GLEnum.TextureMinFilter, GLEnum.Nearest);
                SetParameter(samplerHandle, GLEnum.TextureMagFilter, GLEnum.Nearest);
                var nparam = preset == TexturePreset.NearestClamp ? GLEnum.ClampToEdge : GLEnum.ClampToBorder;
                SetParameter(samplerHandle, GLEnum.TextureWrapS, nparam);
                SetParameter(samplerHandle, GLEnum.TextureWrapT, nparam);
                if (wrapR) SetParameter(samplerHandle, GLEnum.TextureWrapR, nparam);
                break;

            case TexturePreset.NearestRepeat:
                SetParameter(samplerHandle, GLEnum.TextureMinFilter, GLEnum.Nearest);
                SetParameter(samplerHandle, GLEnum.TextureMagFilter, GLEnum.Nearest);
                SetParameter(samplerHandle, GLEnum.TextureWrapS, GLEnum.Repeat);
                SetParameter(samplerHandle, GLEnum.TextureWrapT, GLEnum.Repeat);
                if (wrapR) SetParameter(samplerHandle, GLEnum.TextureWrapR, GLEnum.Repeat);
                break;

            case TexturePreset.LinearClamp:
            case TexturePreset.LinearClampBorder:
                SetParameter(samplerHandle, GLEnum.TextureMinFilter, GLEnum.Linear);
                SetParameter(samplerHandle, GLEnum.TextureMagFilter, GLEnum.Linear);
                var param = preset == TexturePreset.LinearClamp ? GLEnum.ClampToEdge : GLEnum.ClampToBorder;
                SetParameter(samplerHandle, GLEnum.TextureWrapS, param);
                SetParameter(samplerHandle, GLEnum.TextureWrapT, param);
                if (wrapR) SetParameter(samplerHandle, GLEnum.TextureWrapR, param);
                break;

            case TexturePreset.LinearRepeat:
                SetParameter(samplerHandle, GLEnum.TextureMinFilter, GLEnum.Linear);
                SetParameter(samplerHandle, GLEnum.TextureMagFilter, GLEnum.Linear);
                SetParameter(samplerHandle, GLEnum.TextureWrapS, GLEnum.Repeat);
                SetParameter(samplerHandle, GLEnum.TextureWrapT, GLEnum.Repeat);
                if (wrapR) SetParameter(samplerHandle, GLEnum.TextureWrapR, GLEnum.Repeat);
                break;

            case TexturePreset.LinearMipmapClamp:
                SetParameter(samplerHandle, GLEnum.TextureMinFilter, GLEnum.LinearMipmapLinear);
                SetParameter(samplerHandle, GLEnum.TextureMagFilter, GLEnum.Linear);
                SetParameter(samplerHandle, GLEnum.TextureWrapS, GLEnum.ClampToEdge);
                SetParameter(samplerHandle, GLEnum.TextureWrapT, GLEnum.ClampToEdge);
                if (wrapR) SetParameter(samplerHandle, GLEnum.TextureWrapR, GLEnum.ClampToEdge);
                break;

            case TexturePreset.LinearMipmapRepeat:
                SetParameter(samplerHandle, GLEnum.TextureMinFilter, GLEnum.LinearMipmapLinear);
                SetParameter(samplerHandle, GLEnum.TextureMagFilter, GLEnum.Linear);
                SetParameter(samplerHandle, GLEnum.TextureWrapS, GLEnum.Repeat);
                SetParameter(samplerHandle, GLEnum.TextureWrapT, GLEnum.Repeat);
                if (wrapR) SetParameter(samplerHandle, GLEnum.TextureWrapR, GLEnum.Repeat);

                break;

            case TexturePreset.PremultipliedUi:
                SetParameter(samplerHandle, GLEnum.TextureMinFilter, GLEnum.Linear);
                SetParameter(samplerHandle, GLEnum.TextureMagFilter, GLEnum.Linear);
                SetParameter(samplerHandle, GLEnum.TextureWrapS, GLEnum.ClampToEdge);
                SetParameter(samplerHandle, GLEnum.TextureWrapT, GLEnum.ClampToEdge);
                if (wrapR) SetParameter(samplerHandle, GLEnum.TextureWrapR, GLEnum.ClampToEdge);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
        }

        static void SetParameter(NativeHandle handle, GLEnum pName, GLEnum param)
        {
            var intParam = (int)param;
            Gl.SamplerParameterI(handle, pName, ref intParam);
        }
    }
}