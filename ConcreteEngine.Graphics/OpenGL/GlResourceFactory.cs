#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal class GlResourceFactory(GlGraphicsContext gfx)
{
    private readonly GL _gl = gfx.Gl;

    public GlMesh CreateMesh<TVertex, TIndex>(GlGraphicsDevice graphics, MeshDescriptor<TVertex, TIndex> descriptor)
        where TVertex : unmanaged
        where TIndex : unmanaged
    {
        var vertexBufferDesc = descriptor.VertexBuffer;
        var indexBufferDesc = descriptor.IndexBuffer;

        uint handle = _gl.GenVertexArray();
        _gl.BindVertexArray(handle);

        var vertexBuffer = graphics.CreateVertexBuffer(vertexBufferDesc.Usage);
        gfx.BindVertexBuffer(vertexBuffer);
        if (vertexBufferDesc.Data is not null)
            gfx.SetVertexBuffer<TVertex>(vertexBufferDesc.Data.AsSpan());

        ushort indexBuffer = 0;
        if (indexBufferDesc != null)
        {
            indexBuffer = graphics.CreateIndexBuffer(indexBufferDesc.Usage);
            gfx.BindIndexBuffer(indexBuffer);
            if (indexBufferDesc.Data is not null)
                gfx.SetIndexBuffer<TIndex>(indexBufferDesc.Data.AsSpan());
        }

        for (int i = 0; i < descriptor.VertexPointers.Length; i++)
        {
            var pointer = descriptor.VertexPointers[i];
            AddAttribPointer((uint)i, (int)pointer.Format, pointer.StrideBytes, pointer.OffsetBytes,
                pointer.Normalized);
        }

        _gl.BindVertexArray(0);
        gfx.BindVertexBuffer(0);
        gfx.BindIndexBuffer(0);

        if (indexBuffer > 0)
        {
            var indexBufferSize = indexBufferDesc?.Data?.Length ?? 0;
            var drawCount = descriptor.DrawCount.GetValueOrDefault((uint)indexBufferSize);
            var elementSize = Unsafe.SizeOf<TIndex>();
            var elementType = elementSize switch
            {
                1 => DrawElementsType.UnsignedByte,
                2 => DrawElementsType.UnsignedShort,
                3 => DrawElementsType.UnsignedInt,
                _ => throw new GraphicsException($"Index Element Size {elementSize} is not supported")
            };
            return new GlMesh(handle, vertexBuffer, indexBuffer, descriptor.VertexPointers, drawCount, elementType);
        }
        else
        {
            var vertexBufferSize = vertexBufferDesc?.Data?.Length ?? 0;
            var drawCount = descriptor.DrawCount.GetValueOrDefault((uint)vertexBufferSize);
            return new GlMesh(handle, vertexBuffer, descriptor.VertexPointers, drawCount, descriptor.Primitive);
        }
        /*
        var vertexBufferSize = vertexBufferDesc.Data?.Length ?? 0;
        var indexBufferSize = indexBufferDesc?.Data?.Length ?? 0;

        uint drawCount = descriptor.DrawCount.HasValue switch
        {
            true => descriptor.DrawCount.Value,
            _ => (uint)(indexBufferDesc != null ? indexBufferSize : vertexBufferSize)
        };

        var elementSize = Unsafe.SizeOf<TIndex>();
        var elementType = elementSize switch
        {
            1 => DrawElementsType.UnsignedByte,
            2 => DrawElementsType.UnsignedShort,
            3 => DrawElementsType.UnsignedInt,
            _ => throw new GraphicsException($"Index Element Size {elementSize} is not supported")
        };

        var mesh = new GlMesh(handle, vertexBuffer, indexBuffer, descriptor.VertexPointers, drawCount, elementType);
        return mesh;
        */
    }

    public GlBuffer CreateBuffer(BufferTarget target, BufferUsage usage)
    {
        var handle = (ushort)_gl.GenBuffer();

        return target switch
        {
            BufferTarget.VertexBuffer => new GlVertexBuffer(handle, usage),
            BufferTarget.IndexBuffer => new GlIndexBuffer(handle, usage),
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
        };
    }

    public GlTexture2D CreateTexture2D(in TextureDescriptor descriptor)
    {
        uint handle = _gl.GenTexture();
        _gl.BindTexture(GLEnum.Texture2D, handle);
        var (glFormat, glInternalFormat) = descriptor.Format.ToGlEnums();
        unsafe
        {
            fixed (byte* ptr = descriptor.PixelData)
            {
                _gl.TexImage2D(GLEnum.Texture2D, 0, (int)glInternalFormat,
                    (uint)descriptor.Width, (uint)descriptor.Height, 0,
                    glFormat, GLEnum.UnsignedByte, ptr);
            }
        }

        SetTextureParameters(descriptor.Preset);

        _gl.BindTexture(GLEnum.Texture2D, 0);

        var texture = new GlTexture2D(handle, descriptor.Width, descriptor.Height, descriptor.Format);
        return texture;
    }

    public unsafe CreateGlFrameBufferResult CreateFrameBuffer(Vector2D<int> size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size.X,  nameof(size.X));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size.Y,  nameof(size.Y));


        var (width, height) = ((uint)size.X, (uint)size.Y);

        // Color texture
        var textureHandle = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, textureHandle);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, width, height, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, (void*)(0));
        _gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
        _gl.BindTexture(TextureTarget.Texture2D, 0);

        // Depth-stencil renderbuffer
        var renderBufferHandle = _gl.GenRenderbuffer();
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBufferHandle);
        _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, width, height);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

        // Framebuffer
        var fbo = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, textureHandle, 0);
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
            RenderbufferTarget.Renderbuffer, renderBufferHandle);

        //Gl.DrawBuffers(1, GLEnum.ColorAttachment0);
        var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
        {
            GraphicsException.ThrowFramebufferIncomplete(nameof(fbo), status.ToString());
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        return new CreateGlFrameBufferResult(fbo, textureHandle, renderBufferHandle);
    }

    public (GlShader shader, uint handle, UniformTable uniformTable) CreateShader(
        string vertexSource,
        string fragmentSource,
        string[] samplers)
    {
        uint vertexShader = CreateShader(ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentSource);
        uint handle = CreateShaderProgram(vertexShader, fragmentShader);

        var uniformDict = GetUniformsFromProgram(handle);
        var uniformTable = new UniformTable(uniformDict);

        _gl.DetachShader(handle, vertexShader);
        _gl.DetachShader(handle, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        var shader = new GlShader(handle);
        return (shader, handle, uniformTable);
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
            (void*)(offsetBytes)
        );
    }

    private void SetTextureParameters(TexturePreset preset)
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
                break;

            case TexturePreset.LinearMipmapRepeat:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
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