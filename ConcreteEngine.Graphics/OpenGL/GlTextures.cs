using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlTextures: IGraphicsDriverModule
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
    private GlTextureHandle GetTexHandle(in GfxHandle handle) => _store.Texture.Get(in handle);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindTexture(in GfxHandle handle, uint slot)
    {
        _gl.BindTextureUnit(GetTexHandle(in handle).Handle, slot);
    }


    public ResourceRefToken<TextureId> CreateTexture2D(uint width, uint height, uint mipLevels)
    {
        var levels = Math.Min(1, mipLevels);
        _gl.CreateTextures(TextureTarget.Texture2D, 1, out uint texture);
        _gl.TextureStorage2D(texture, levels, ColorFormat, width, height);
        return _store.Texture.Add(new GlTextureHandle(texture));
    }

    public ResourceRefToken<TextureId> CreateTextureCubeMap(uint width, uint height, uint mipLevels)
    {
        var levels = Math.Min(1, mipLevels);
        _gl.CreateTextures(TextureTarget.TextureCubeMap, 1, out uint texture);
        _gl.TextureStorage2D(texture, levels, ColorFormat, width, height);
        return _store.Texture.Add(new GlTextureHandle(texture));
    }

    private void CreateTextureStore(in GfxHandle texture, uint width, uint height, uint mipLevels)
    {
        var handle = GetTexHandle(in texture).Handle;
        var levels = Math.Min(1, mipLevels);
        _gl.TextureStorage2D(handle, levels, ColorFormat, width, height);
    }
    
    public void UploadTextureData(in GfxHandle texture, GpuTextureData data)
    {
        var handle = GetTexHandle(in texture).Handle;
        _gl.TextureSubImage2D(handle, 0, 0, 0, data.Width, data.Height, 
            PixelFormat.Rgba, PixelType.UnsignedByte, data.PixelData);
    }

    public unsafe void UploadTextureEmptyData(in GfxHandle texture)
    {
        var handle = GetTexHandle(in texture).Handle;
        _gl.TextureSubImage2D(handle, 0, 0, 0, 1, 1, 
            PixelFormat.Rgba, PixelType.UnsignedByte, (void*)0);
    }

    public void UploadCubeMapFaceData(in GfxHandle texture, GpuTextureData data, int faceIdx)
    {
        var handle = GetTexHandle(in texture).Handle;
        _gl.TextureSubImage3D(
            handle, level: 0,
            xoffset: 0, yoffset: 0, zoffset: faceIdx,
            width: data.Width, height: data.Height, depth: 1,
            format: PixelFormat.Rgba, type: PixelType.UnsignedByte,
            pixels: data.PixelData 
        );
    }

    public void SetTexturePreset(in GfxHandle texture, TexturePreset preset)
    {
        var handle = GetTexHandle(in texture);

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
                _gl.GenerateTextureMipmap(handle.Handle);
                break;

            case TexturePreset.LinearMipmapRepeat:
                SetTexParameter(GLEnum.TextureWrapS, GLEnum.Repeat);
                SetTexParameter(GLEnum.TextureWrapT, GLEnum.Repeat);
                SetTexParameter(GLEnum.TextureMinFilter, GLEnum.LinearMipmapLinear);
                SetTexParameter(GLEnum.TextureMagFilter, GLEnum.Linear);
                _gl.GenerateTextureMipmap(handle.Handle);
                break;

            case TexturePreset.PremultipliedUI:
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

    public void SetLodBias(in GfxHandle texture, float lodBias)
        => _gl.TextureParameter(GetTexHandle(in texture).Handle, GLEnum.TextureLodBias, lodBias);

    public void SetAnisotropy(in GfxHandle texture, int anisotropy)
        => _gl.TextureParameter(GetTexHandle(in texture).Handle, GLEnum.TextureLodBias, anisotropy);


    public GlTextureHandle CreateTexture2DASD(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta)
    {
        var handle = _gl.GenTexture();
        _gl.BindTexture(GLEnum.Texture2D, handle);
        var (glFormat, glInternalFormat) = desc.Format.ToGlEnums();

        unsafe
        {
            if (desc.NullPtrData)
            {
                _gl.TexImage2D(GLEnum.Texture2D, 0, (int)glInternalFormat,
                    (uint)desc.Width, (uint)desc.Height, 0,
                    glFormat, GLEnum.UnsignedByte, (void*)0);
            }
            else
            {
                _gl.TexImage2D(GLEnum.Texture2D, 0, (int)glInternalFormat,
                    (uint)desc.Width, (uint)desc.Height, 0,
                    glFormat, GLEnum.UnsignedByte, data.PixelData);
            }
        }

        SetTextureParameters(desc.Preset, desc.Anisotropy, desc.LodBias);

        _gl.BindTexture(GLEnum.Texture2D, 0);

        meta = new TextureMeta(desc.Width, desc.Height, desc.Format);
        return new GlTextureHandle(handle);
    }

    public unsafe GlTextureHandle CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta)
    {
        var (width, height) = (desc.Width, desc.Height);

        if (width != height)
            throw new InvalidOperationException("Width and Height are not the same size");

        if (width != desc.Width || height != desc.Height)
            throw new InvalidOperationException("Miss match between cubemap size");

        var target = (int)TextureTarget.TextureCubeMapPositiveX;

        var handle = _gl.GenTexture();
        _gl.BindTexture(GLEnum.TextureCubeMap, handle);
        var (format, internalFormat) = desc.Format.ToGlEnums();

        CreateFace(data.FaceData1, 0);
        CreateFace(data.FaceData2, 1);
        CreateFace(data.FaceData3, 2);
        CreateFace(data.FaceData4, 3);
        CreateFace(data.FaceData5, 4);
        CreateFace(data.FaceData6, 5);

        _gl.TextureParameterI(handle.Handle, pname, (int)param)
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        _gl.BindTexture(TextureTarget.TextureCubeMap, 0);

        meta = new TextureMeta(width, height, desc.Format);
        return new GlTextureHandle(handle);

        void CreateFace(ReadOnlySpan<byte> faceData, int face)
        {
            _gl.TexImage2D((TextureTarget)(target + face), 0, (int)internalFormat,
                (uint)width, (uint)height, 0,
                format, GLEnum.UnsignedByte, faceData);
        }
    }


    private void SetTextureParameters(in GfxHandle texture, TexturePreset preset)
    {
    }

    /*
     public void GenerateMipmap(in GfxHandle texture)
         => _gl.GenerateTextureMipmap(GetTexHandle(in texture).Handle);
     */
}