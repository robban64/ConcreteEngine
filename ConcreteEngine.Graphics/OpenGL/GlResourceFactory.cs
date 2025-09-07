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

    private void SetBufferData<TData>(BufferTargetARB target, BufferUsageARB usage, ReadOnlySpan<TData> data,
        int? dataLength = null)
        where TData : unmanaged
    {
        var elementSize = Unsafe.SizeOf<TData>();
        var length = dataLength.GetValueOrDefault(data.Length);
        var size = length * elementSize;
        _gl.BufferData(target, (nuint)size, data, usage);
    }

    public GlMeshHandle CreateMesh<TVertex, TIndex>(
        Func<GlVertexBufferHandle, VertexBufferMeta, VertexBufferId> vboHandler,
        Func<GlIndexBufferHandle, IndexBufferMeta, IndexBufferId> iboHandler,
        in MeshDataDescriptor<TVertex, TIndex> dataDesc,
        in MeshMetaDescriptor metaDesc,
        out MeshMeta meta
    ) where TVertex : unmanaged where TIndex : unmanaged
    {
        var handle = _gl.GenVertexArray();
        _gl.BindVertexArray(handle);

        var vbo = CreateVertexBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo.Handle);

        if (!dataDesc.Vertices.IsEmpty)
            SetBufferData<TVertex>(BufferTargetARB.ArrayBuffer, metaDesc.VboUsage.ToGlEnum(), dataDesc.Vertices,
                dataDesc.Vertices.Length);

        GlIndexBufferHandle ibo = default;

        if (dataDesc.Elemental)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(dataDesc.Indices.Length, 0);

            ibo = CreateIndexBuffer();
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ibo.Handle);
            if (!dataDesc.Indices.IsEmpty)
                SetBufferData<TIndex>(BufferTargetARB.ElementArrayBuffer, metaDesc.IboUsage.ToGlEnum(),
                    dataDesc.Indices, dataDesc.Indices.Length);
        }

        for (int i = 0; i < metaDesc.VertexPointers.Length; i++)
        {
            var pointer = metaDesc.VertexPointers[i];
            AddAttribPointer((uint)i, (int)pointer.Format, pointer.StrideBytes, pointer.OffsetBytes,
                pointer.Normalized);
        }

        _gl.BindVertexArray(0);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);


        var vp = metaDesc.VertexPointers;
        var pointer1 = vp[0];
        var pointer2 = vp.Length > 1 ? vp[1] : default;
        var pointer3 = vp.Length > 2 ? vp[2] : default;

        uint drawCount = 0;
        bool isStatic = true;

        var (vboSize, vboElementSize) = ((uint)(dataDesc.Vertices.Length), (uint)Unsafe.SizeOf<TIndex>());

        IndexBufferId iboId = default;
        IboElementType elementType = IboElementType.Invalid;

        if (ibo.Handle > 0 && dataDesc.Elemental)
        {
            var iboSize = (uint)dataDesc.Indices.Length;
            var iboElementSize = (uint)Unsafe.SizeOf<TIndex>();
            elementType = iboElementSize switch
            {
                1 => IboElementType.UnsignedByte,
                2 => IboElementType.UnsignedShort,
                4 => IboElementType.UnsignedInt,
                _ => throw GraphicsException.UnsupportedFeature($"Index Element Size {iboElementSize}")
            };

            drawCount = metaDesc.DrawCount.GetValueOrDefault(iboSize);
            isStatic = metaDesc.IboUsage == BufferUsage.StaticDraw;
            var iboMeta = new IndexBufferMeta(metaDesc.IboUsage, iboSize, iboElementSize);
            iboId = iboHandler(ibo, iboMeta);
        }
        else
        {
            isStatic = metaDesc.VboUsage == BufferUsage.StaticDraw;
            drawCount = metaDesc.DrawCount.GetValueOrDefault(vboSize);
        }

        var vboId = vboHandler(vbo, new VertexBufferMeta(metaDesc.VboUsage, vboSize, vboElementSize));

        var drawKind = metaDesc.DrawKind;
        var primitive = metaDesc.Primitive ?? DrawPrimitive.Triangles;
        if (drawKind == MeshDrawKind.Arrays && iboId.Id > 0)
            GraphicsException.ThrowInvalidState("MeshDraw is arrays but has index buffer");

        meta = new MeshMeta(vboId, iboId, primitive, elementType, drawKind, isStatic,
            drawCount, (uint)vp.Length,
            pointer1, pointer2, pointer3);

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

    public GlTextureHandle CreateTexture2D(in TextureDesc desc, out TextureMeta meta)
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
                fixed (byte* ptr = desc.PixelData)
                {
                    _gl.TexImage2D(GLEnum.Texture2D, 0, (int)glInternalFormat,
                        (uint)desc.Width, (uint)desc.Height, 0,
                        glFormat, GLEnum.UnsignedByte, ptr);
                }
            }
        }

        SetTextureParameters(desc.Preset, desc.Anisotropy, desc.LodBias);

        _gl.BindTexture(GLEnum.Texture2D, 0);

        meta = new TextureMeta(desc.Width, desc.Height, desc.Format);
        return new GlTextureHandle(handle);
    }

    public unsafe GlTextureHandle CreateCubeMap(in CreateCubemapDesc cubemapDesc, out TextureMeta meta)
    {
        var loaders = cubemapDesc.Loaders;
        var (width, height) = (cubemapDesc.Width, cubemapDesc.Height);

        ArgumentNullException.ThrowIfNull(loaders);
        ArgumentOutOfRangeException.ThrowIfLessThan(loaders.Length, 0, nameof(loaders));

        var target = (int)TextureTarget.TextureCubeMapPositiveX;

        var handle = _gl.GenTexture();
        _gl.BindTexture(GLEnum.TextureCubeMap, handle);

        for (int i = 0; i < 6; i++)
        {
            var result = loaders[i]();
            var (format, internalFormat) = result.Format.ToGlEnums();

            if (width != height)
                throw new InvalidOperationException("Width and Height are not the same size");

            if (width != result.Width || height != result.Height)
                throw new InvalidOperationException("Miss match between cubemap size");

            fixed (byte* ptr = result.PixelData)
            {
                _gl.TexImage2D((TextureTarget)(target + i), 0, (int)internalFormat,
                    (uint)width, (uint)height, 0,
                    format, GLEnum.UnsignedByte, ptr);
            }
        }

        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        _gl.BindTexture(TextureTarget.TextureCubeMap, 0);

        meta = new TextureMeta(width, height, cubemapDesc.Format);
        return new GlTextureHandle(handle);
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
        Func<GlTextureHandle, TextureMeta, TextureId> textureHandler,
        Func<GlRenderBufferHandle, RenderBufferMeta, RenderBufferId> rboTexHandler,
        Func<GlRenderBufferHandle, RenderBufferMeta, RenderBufferId> rboDepthHandler,
        Vector2D<int> viewport,
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
            colTexHandle = CreateTexture2D(new TextureDesc([], size.X, size.Y, EnginePixelFormat.Rgba,
                desc.TexturePreset, NullPtrData: true), out colTexMeta);

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

        var colTexId = colTexHandle.Handle > 0 ? textureHandler(colTexHandle, colTexMeta) : default;
        var rboTexId = rboTexHandle.Handle > 0 ? rboTexHandler(rboTexHandle, rboTexMeta) : default;
        var rboDepthId = rboDepthHandle.Handle > 0 ? rboDepthHandler(rboDepthHandle, rboDepthMeta) : default;

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