#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlResourceFactory(GlGraphicsContext gfx, DeviceCapabilities caps)
{
    private readonly GL _gl = gfx.Gl;
    private readonly DeviceCapabilities _caps = caps;


    public GlMeshHandle CreateVao()
    {
        var handle = _gl.GenVertexArray();
        return new GlMeshHandle(handle);
    }

    public GlVertexBufferHandle CreateVertexBuffer()
    {
        var handle = _gl.GenBuffer();
        return new GlVertexBufferHandle(handle);
    }

    public GlIndexBufferHandle CreateIndexBuffer()
    {
        var handle = _gl.GenBuffer();
        return new GlIndexBufferHandle(handle);
    }

    public GlTextureHandle CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta)
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

    private GlRenderBufferHandle CreateRenderBufferForFbo(RenderBufferKind kind, Vector2D<int> size, bool multisample,
        uint samples, out RenderBufferMeta meta)
    {
        var (width, height) = ((uint)size.X, (uint)size.Y);
        var (format, attachment) = kind switch
        {
            RenderBufferKind.Color => (InternalFormat.Rgba8, FramebufferAttachment.ColorAttachment0),
            RenderBufferKind.DepthStencil => (InternalFormat.Depth24Stencil8,
                FramebufferAttachment.DepthStencilAttachment),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        var handle = _gl.GenRenderbuffer();
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, handle);

        if (multisample)
            _gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, format, width, height);
        else
            _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, format, width, height);

        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachment, RenderbufferTarget.Renderbuffer, handle);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

        meta = new RenderBufferMeta(kind, size, multisample);

        return new GlRenderBufferHandle(handle);
    }

    public GlFrameBufferHandle CreateFrameBuffer(
        UploadFboDel<TextureId, TextureMeta, GlTextureHandle> texCallback,
        UploadFboDel<RenderBufferId, RenderBufferMeta, GlRenderBufferHandle> rboCallback,
        Vector2D<int> viewport,
        in FrameBufferMeta previousMeta,
        in FrameBufferDesc desc,
        out FrameBufferMeta meta
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(viewport.X, nameof(viewport.X));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(viewport.Y, nameof(viewport.Y));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(desc.SizeRatio.X, nameof(desc.SizeRatio.X));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(desc.SizeRatio.Y, nameof(desc.SizeRatio.Y));


        var size = new Vector2D<int>((int)(viewport.X * desc.SizeRatio.X), (int)(viewport.Y * desc.SizeRatio.Y));

        GlTextureHandle colTexHandle = default;
        TextureMeta colTexMeta = default;

        GlRenderBufferHandle rboTexHandle = default, rboDepthHandle = default;
        RenderBufferMeta rboTexMeta = default, rboDepthMeta = default;


        var fbo = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        if (desc.Msaa)
        {
            rboTexHandle = CreateRenderBufferForFbo(RenderBufferKind.Color, size, true, desc.Samples, out rboTexMeta);
            if (desc.DepthStencilBuffer)
            {
                rboDepthHandle = CreateRenderBufferForFbo(RenderBufferKind.DepthStencil, size, true, desc.Samples,
                    out rboDepthMeta);
            }
        }
        else
        {
            var texDesc = new GpuTextureDescriptor(size.X, size.Y, EnginePixelFormat.Rgba,
                desc.TexturePreset, NullPtrData: true);
            colTexHandle = CreateTexture2D(new GpuTextureData(ReadOnlySpan<byte>.Empty), in texDesc, out colTexMeta);

            _gl.BindTexture(TextureTarget.Texture2D, colTexHandle.Handle);
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, colTexHandle.Handle, 0);

            // Depth-stencil renderbuffer (single-sample)
            if (desc.DepthStencilBuffer)
            {
                rboDepthHandle =
                    CreateRenderBufferForFbo(RenderBufferKind.DepthStencil, size, false, 0, out rboDepthMeta);
            }

            _gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        _gl.DrawBuffers(1, GLEnum.ColorAttachment0);
        var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
        {
            GraphicsException.ThrowFramebufferIncomplete(nameof(fbo), status.ToString());
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        var colTexId = colTexHandle.Handle > 0
            ? texCallback(previousMeta.ColTexId, in colTexMeta, colTexHandle)
            : default;

        var rboTexId = rboTexHandle.Handle > 0
            ? rboCallback(previousMeta.RboTexId, in rboTexMeta, rboTexHandle)
            : default;

        var rboDepthId = rboDepthHandle.Handle > 0
            ? rboCallback(previousMeta.RboDepthId, in rboDepthMeta, rboDepthHandle)
            : default;

        meta = new FrameBufferMeta(
            colTexId, rboTexId, rboDepthId, desc.TexturePreset,
            desc.SizeRatio, size,
            desc.DepthStencilBuffer, desc.Msaa, (byte)desc.Samples
        );

        return new GlFrameBufferHandle(fbo);
    }


    private unsafe void AddAttribPointer(uint index, int size, uint strideBytes, uint offsetBytes,
        bool normalized = false)
    {
        _gl.EnableVertexAttribArray(index);
        _gl.VertexAttribPointer(
            index,
            size,
            VertexAttribPointerType.Float,
            normalized,
            strideBytes,
            (void*)offsetBytes
        );
    }

    private void SetTextureParameters(TexturePreset preset, TextureAnisotropy anisotropy, float lodBias)
    {
        switch (preset)
        {
            case TexturePreset.NearestClamp:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
                break;

            case TexturePreset.NearestRepeat:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
                break;

            case TexturePreset.LinearClamp:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                break;

            case TexturePreset.LinearRepeat:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                break;

            case TexturePreset.LinearMipmapClamp:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                _gl.GenerateMipmap(TextureTarget.Texture2D);
                break;

            case TexturePreset.LinearMipmapRepeat:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                _gl.GenerateMipmap(TextureTarget.Texture2D);
                break;

            case TexturePreset.PremultipliedUI:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                // Could add sRGB decode disable if doing manual gamma
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
        }

        bool isMipMap = preset == TexturePreset.LinearMipmapClamp || preset == TexturePreset.LinearMipmapRepeat;
        if (isMipMap)
        {
            var anisotropyValue = GetAnisotropy(anisotropy);
            if (anisotropyValue > 1)
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMaxAnisotropy, anisotropyValue);

            if (lodBias != 0)
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureLodBias, lodBias);
        }
    }

    private float GetAnisotropy(TextureAnisotropy anisotropy)
    {
        int value = anisotropy switch
        {
            TextureAnisotropy.Off => 0,
            TextureAnisotropy.Default => 4,
            TextureAnisotropy.X2 => 2,
            TextureAnisotropy.X4 => 4,
            TextureAnisotropy.X8 => 8,
            TextureAnisotropy.X16 => 16,
            _ => throw new ArgumentOutOfRangeException(nameof(anisotropy), anisotropy, null),
        };

        return Math.Min(value, caps.MaxAnisotropy);
    }
}