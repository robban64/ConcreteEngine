#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal class GlResourceFactory(GlGraphicsContext gfx)
{
    private readonly GL _gl = gfx.Gl;

    private void SetBufferData<TData>(BufferTargetARB target, BufferUsageARB usage, ReadOnlySpan<TData> data)
        where TData : unmanaged
    {
        var elementSize = Unsafe.SizeOf<TData>();
        var size = data.Length * elementSize;
        _gl.BufferData(target, (nuint)size, data, usage);
    }

    public GlMeshHandle CreateMesh<TVertex, TIndex>(
        Func<GlVertexBufferHandle, VertexBufferMeta, VertexBufferId> vboHandler,
        Func<GlIndexBufferHandle, IndexBufferMeta, IndexBufferId> iboHandler,
        MeshDescriptor<TVertex, TIndex> descriptor,
        out MeshMeta meta)
        where TVertex : unmanaged
        where TIndex : unmanaged
    {
        var vboDesc = descriptor.VertexBuffer;
        var iboDesc = descriptor.IndexBuffer;

        var handle = _gl.GenVertexArray();
        _gl.BindVertexArray(handle);

        var vbo = CreateVertexBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo.Handle);

        if (vboDesc.Data is not null)
            SetBufferData<TVertex>(BufferTargetARB.ArrayBuffer, vboDesc.Usage.ToGlEnum(), vboDesc.Data);

        GlIndexBufferHandle ibo = default;
        if (iboDesc != null)
        {
            ibo = CreateIndexBuffer();
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ibo.Handle);
            if (iboDesc.Data is not null)
                SetBufferData<TIndex>(BufferTargetARB.ElementArrayBuffer, iboDesc.Usage.ToGlEnum(), iboDesc.Data);
        }

        for (int i = 0; i < descriptor.VertexPointers.Length; i++)
        {
            var pointer = descriptor.VertexPointers[i];
            AddAttribPointer((uint)i, (int)pointer.Format, pointer.StrideBytes, pointer.OffsetBytes,
                pointer.Normalized);
        }

        _gl.BindVertexArray(0);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);


        var vp = descriptor.VertexPointers;
        var pointer1 = vp[0];
        var pointer2 = vp.Length > 1 ? vp[1] : default;
        var pointer3 = vp.Length > 2 ? vp[2] : default;

        uint drawCount = 0;
        bool isStatic = true;

        var (vboSize, vboElementSize) = ((uint)(vboDesc.Data?.Length ?? 0), (uint)Unsafe.SizeOf<TIndex>());

        IndexBufferId iboId = default;
        IboElementType elementType = IboElementType.Invalid;

        if (ibo.Handle > 0 && iboDesc != null)
        {
            var iboSize = (uint)(iboDesc.Data?.Length ?? 0);
            var iboElementSize = (uint)Unsafe.SizeOf<TIndex>();
            elementType = iboElementSize switch
            {
                1 => IboElementType.UnsignedByte,
                2 => IboElementType.UnsignedShort,
                3 => IboElementType.UnsignedInt,
                _ => throw GraphicsException.UnsupportedFeature($"Index Element Size {iboElementSize}")
            };

            drawCount = descriptor.DrawCount.GetValueOrDefault(iboSize);
            isStatic = iboDesc.Usage == BufferUsage.StaticDraw;
            var iboMeta = new IndexBufferMeta(iboDesc.Usage, iboSize, iboElementSize);
            iboId = iboHandler(ibo, iboMeta);
        }
        else
        {
            isStatic = descriptor.VertexBuffer.Usage == BufferUsage.StaticDraw;
            drawCount = descriptor.DrawCount.GetValueOrDefault(vboSize);
        }

        var vboId = vboHandler(vbo, new VertexBufferMeta(vboDesc.Usage, vboSize, vboElementSize));


        meta = new MeshMeta(vboId, iboId, descriptor.Primitive, elementType,
            drawCount, isStatic, (uint)vp.Length,
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

        SetTextureParameters(desc.Preset, desc.LodBias);
        _gl.BindTexture(GLEnum.Texture2D, 0);

        meta = new TextureMeta(new Vector2D<int>(desc.Width, desc.Height), desc.Format);
        return new GlTextureHandle(handle);
    }

    private GlRenderBufferHandle CreateRenderBufferForFbo(RenderBufferKind kind, Vector2D<int> size, bool multisample,
        uint samples, out RenderBufferMeta meta)
    {
        /*
 *         if (desc.Msaa)
        {
            // Renderbuffer texture
            rboTexHandle = _gl.GenRenderbuffer();
            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rboTexHandle);
            _gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, desc.Samples, InternalFormat.Rgba8,width, height);
            _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                RenderbufferTarget.Renderbuffer, rboTexHandle);

            // Depth-stencil renderbuffer (multisampled)
            if (desc.DepthStencilBuffer)
            {
                depthStencilRboHandle = _gl.GenRenderbuffer();
                _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthStencilRboHandle);
                _gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, desc.Samples, InternalFormat.Depth24Stencil8, width, height);
                _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
                    RenderbufferTarget.Renderbuffer, depthStencilRboHandle);
            }

            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        }
 */

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
                TexturePreset.LinearClamp, NullPtrData: true), out colTexMeta);

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
            colTexId, rboTexId, rboDepthId,
            desc.SizeRatio, size,
            desc.DepthStencilBuffer, desc.Msaa, (byte)desc.Samples
        );

        return new GlFrameBufferHandle(fbo);
    }


    public GlShaderHandle CreateShader(
        string vertexSource,
        string fragmentSource,
        string[] samplers,
        out UniformTable uniformTable,
        out ShaderMeta meta
    )
    {
        uint vertexShader = CreateShader(ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentSource);
        uint handle = CreateShaderProgram(vertexShader, fragmentShader);

        var uniformDict = GetUniformsFromProgram(handle);
        uniformTable = new UniformTable(uniformDict);

        _gl.DetachShader(handle, vertexShader);
        _gl.DetachShader(handle, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        meta = new ShaderMeta((uint)samplers.Length);
        return new GlShaderHandle(handle);
    }

    private uint CreateShaderProgram(uint vertexShader, uint fragmentShader)
    {
        uint program = _gl.CreateProgram();
        _gl.AttachShader(program, vertexShader);
        _gl.AttachShader(program, fragmentShader);
        _gl.LinkProgram(program);

        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus != (int)GLEnum.True)
            throw GraphicsException.ShaderLinkFailed(program.ToString(), _gl.GetProgramInfoLog(program));

        return program;
    }

    private uint CreateShader(ShaderType shaderType, string source)
    {
        uint shader = _gl.CreateShader(shaderType);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int)GLEnum.True)
            throw GraphicsException.ShaderCompileFailed(nameof(shaderType), _gl.GetShaderInfoLog(shader));

        return shader;
    }

    private Dictionary<string, short> GetUniformsFromProgram(uint handle)
    {
        var uniforms = new Dictionary<string, short>();

        _gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out int uniformsLength);
        for (uint uniformIndex = 0; uniformIndex < uniformsLength; uniformIndex++)
        {
            string uniformName = _gl.GetActiveUniform(handle, uniformIndex, out _, out _);
            int uniformLocation = _gl.GetUniformLocation(handle, uniformName);
            if (uniformLocation >= 0)
            {
                uniforms.Add(uniformName, (short)uniformLocation);
            }
        }

        return uniforms;
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

    private void SetTextureParameters(TexturePreset preset, float lodBias)
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
                if (lodBias != 0) _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureLodBias, lodBias);
                break;

            case TexturePreset.LinearMipmapRepeat:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                _gl.GenerateMipmap(TextureTarget.Texture2D);
                if (lodBias != 0) _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureLodBias, lodBias);
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
    }
}