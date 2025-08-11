#region

using System.Drawing;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlGraphicsContext : IGraphicsContext
{
    private readonly GL _gl;
    private readonly int glMinor = 0;
    private readonly int glMajor = 0;

    private GlShader? _currentProgram;
    private GlVertexBuffer? _boundVertexBuffer;
    private GlIndexBuffer? _boundIndexBuffer;
    private GlMesh? _boundVao;
    private uint[] _boundTextures;
    

    private float _deltaTime = 0f;
    private Vector2D<int> _viewPortSize = Vector2D<int>.Zero;
    private Vector2D<int> _framebufferSize = Vector2D<int>.Zero;

    private int _drawTriangleCount = 0;
    private int _drawCallCount = 0;
    public GraphicsConfiguration Configuration { get; }
    public ViewTransform2D ViewTransform { get; }
    
    public Vector2D<int> FramebufferSize => _framebufferSize;
    public Vector2D<int> ViewportSize => _viewPortSize;

    public GL Gl => _gl;


    internal GlGraphicsContext(GL gl, GraphicsConfiguration configuration, in RenderFrameContext initialFrameCtx)
    {
        _gl = gl;
        Configuration = configuration;

        _boundTextures = new uint[configuration.MaxTextureImageUnits];

        _framebufferSize = initialFrameCtx.FramebufferSize;
        //_renderPipeline = new RenderPipeline(this);
        ViewTransform = new ViewTransform2D
        {
            Position = Vector2D<float>.Zero,
            Rotation = 0f,
            Zoom = 1f,
            ViewportSize = initialFrameCtx.ViewportSize,
        };

        gl.GetInteger(GetPName.MajorVersion, out glMajor);
        gl.GetInteger(GetPName.MinorVersion, out glMinor);
        int glVersion = glMajor * 100 + glMinor * 10;
        Console.WriteLine($"OpenGL version {glVersion} loaded ({glMajor}.{glMinor})");
        
        SetBlendMode(BlendMode.Alpha);
        _gl.Disable(GLEnum.CullFace);
    }

    public void Begin(in RenderFrameContext frameCtx)
    {
        _deltaTime = frameCtx.DeltaTime;
        _framebufferSize = frameCtx.FramebufferSize;
        _viewPortSize = frameCtx.ViewportSize;

        _drawCallCount = 0;
        _drawTriangleCount = 0;
    }

    public void BeginRender()
    {
        _gl.Viewport(_viewPortSize);
        Clear(Color.CornflowerBlue);
    }

    public void End()
    {
        // unbind context
        UseShader(null);
        BindMesh(null);
        BindVertexBuffer(null);
        BindIndexBuffer(null);
        for (uint i = 0; i < _boundTextures.Length; i++)
        {
            BindTexture(i, null);
        }
    }

    public void Clear(Color color)
    {
        _gl.ClearColor(color);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void UseShader(IShader? shader)
    {
        if (shader is null)
        {
            _currentProgram = null;
            _gl.UseProgram(0);
            return;
        }

        if (shader is not GlShader glShader)
            throw GraphicsException.InvalidType<GlShader>(nameof(shader), shader);

        if (_currentProgram == glShader) return;
        _currentProgram = glShader;
        _gl.UseProgram(glShader.Handle);
    }

    public void BindTexture(uint slot, ITexture2D? texture)
    {
        if ((int)slot >= Configuration.MaxTextureImageUnits)
            throw GraphicsException.CapabilityExceeded<GlTexture2D>("Texture slot", (int)slot,
                Configuration.MaxTextureImageUnits);

        if (texture is null)
        {
            _gl.BindTextureUnit(slot, 0);

            _boundTextures[slot] = 0;
            return;
        }

        if (texture is not GlTexture2D glTex)
            throw GraphicsException.InvalidType<GlTexture2D>(nameof(texture), texture);


        if (_boundTextures[slot] == glTex.Handle) return;

        _gl.BindTextureUnit(slot, glTex.Handle);
        _boundTextures[slot] = glTex.Handle;
    }

    public void BindMesh(IMesh? mesh)
    {
        if (mesh is null)
        {
            _gl.BindVertexArray(0);
            _boundVao = null;
            return;
        }

        if (mesh is not GlMesh glMesh)
            throw GraphicsException.InvalidType<GlMesh>(nameof(mesh), mesh);

        _gl.BindVertexArray(glMesh.Handle);
        _boundVao = glMesh;
    }

    public void BindVertexBuffer(IGraphicsBuffer? buffer)
    {
        if (buffer is null)
        {
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _boundVertexBuffer = null;
            return;
        }

        if (buffer is not GlVertexBuffer glBuffer)
            throw GraphicsException.InvalidType<GlVertexBuffer>(nameof(buffer), buffer);

        _gl.BindBuffer(glBuffer.GlBufferTarget, glBuffer.Handle);
        _boundVertexBuffer = glBuffer;
    }

    public void BindIndexBuffer(IGraphicsBuffer? buffer)
    {
        if (buffer is null)
        {
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
            _boundIndexBuffer = null;
            return;
        }

        if (buffer is not GlIndexBuffer glBuffer)
            throw GraphicsException.InvalidType<GlIndexBuffer>(nameof(buffer), buffer);

        _gl.BindBuffer(glBuffer.GlBufferTarget, glBuffer.Handle);
        _boundIndexBuffer = glBuffer;
    }

    public void SetVertexBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        SetBufferData(_boundVertexBuffer, data);
    }

    public void SetIndexBuffer(ReadOnlySpan<uint> data)
    {
        SetBufferData(_boundIndexBuffer, data);
    }

    private void SetBufferData<T>(GlBuffer? buffer, ReadOnlySpan<T> data) where T : unmanaged
    {
        ValidateBoundResource(buffer);
        if (buffer!.IsStatic && buffer.BufferSizeInBytes > 0)
            throw GraphicsException.InvalidBufferData<GlBuffer>(nameof(buffer), "Buffer is static");

        buffer.ElementCount = data.Length;
        buffer.ElementSize = Unsafe.SizeOf<T>();
        Gl.BufferData(buffer.GlBufferTarget, (nuint)buffer.BufferSizeInBytes, data, buffer.GlBufferUsage);

        CheckGlError();
    }

    public void UploadVertexBuffer<T>(ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        UploadBufferData(_boundVertexBuffer, data, offsetElements);
    }

    public void UploadIndexBuffer(ReadOnlySpan<uint> data, int offsetElements)
    {
        UploadBufferData(_boundIndexBuffer, data, offsetElements);
    }

    private void UploadBufferData<T>(GlBuffer? buffer, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offsetElements, nameof(offsetElements));
        ValidateBoundResource(buffer);

        if (buffer.IsStatic)
            throw GraphicsException.InvalidBufferData<GlBuffer>(nameof(buffer), "Buffer is static");

        var elementSize = Unsafe.SizeOf<T>();

        if (elementSize != buffer.ElementSize)
            throw GraphicsException.InvalidBufferData<GlBuffer>(nameof(elementSize), "Invalid element size");

        if (data.Length + offsetElements > buffer.ElementCount)
        {
            throw GraphicsException.InvalidBufferData<GlBuffer>(null,
                $"Upload data {data.Length + offsetElements} cannot be bigger than {buffer.ElementCount} elements.");
        }


        nint byteOffset = (nint)((long)offsetElements * elementSize);
        Gl.BufferSubData(buffer.GlBufferTarget, byteOffset, data);

        CheckGlError(); // throws here
    }

    public void Draw(uint vertexCount)
    {
        _gl.DrawArrays(PrimitiveType.Triangles, 0, vertexCount);
        _drawTriangleCount += (int)vertexCount;
        _drawCallCount++;
    }

    public unsafe void DrawIndexed(uint indexCount)
    {
        _gl.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, (void*)0);
        _drawTriangleCount += (int)indexCount;
        _drawCallCount++;
    }

    public void BindDefaultFramebuffer()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void SetBlendMode(BlendMode blendMode)
    {
        if (blendMode == BlendMode.Alpha)
        {
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }
        else if (blendMode == BlendMode.Additive)
        {
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
        }
        else if (blendMode == BlendMode.None)
        {
            _gl.Disable(EnableCap.Blend);
        }
    }

    public IRenderTarget CreateRenderTarget()
    {
        return new GlRenderTarget(this);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, int value) => _gl.Uniform1(_currentProgram![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, uint value) => _gl.Uniform1(_currentProgram![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, float value) => _gl.Uniform1(_currentProgram![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector2D<float> value) =>
        _gl.Uniform2(_currentProgram![uniform], value.X, value.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector3D<float> value)
    {
        _gl.Uniform3(_currentProgram![uniform], value.X, value.Y, value.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector4D<float> value)
    {
        _gl.Uniform4(_currentProgram![uniform], value.X, value.Y, value.Z, value.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetUniform(ShaderUniform uniform, in Matrix4X4<float> value)
    {
        var p = (float*)Unsafe.AsPointer(ref Unsafe.AsRef(in value));
        _gl.UniformMatrix4(_currentProgram![uniform], 1, false, p);
    }


    private void CheckGlError()
    {
        var error = Gl.GetError();
        if (error != (GLEnum)ErrorCode.NoError)
            throw new OpenGlException(error);
    }

    private static void ValidateBoundResource<T>(T? resource) where T : OpenGLResource
    {
        if (resource is null)
            throw GraphicsException.ResourceNotBound<T>(nameof(resource));

        if (resource.Handle == 0)
            throw GraphicsException.MissingHandle<T>(nameof(resource));

        if (resource.IsDisposed)
            throw GraphicsException.ResourceIsDisposed<T>(nameof(resource));
    }
}