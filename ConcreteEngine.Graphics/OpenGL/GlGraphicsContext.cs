#region

using System.Diagnostics.CodeAnalysis;
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
    
    private readonly GraphicsResourceStore _store;

    private ushort _boundShader = 0;
    private ushort _boundVertexBuffer  = 0;
    private ushort _boundIndexBuffer  = 0;
    private ushort _boundVao = 0;
    private readonly ushort[] _boundTextures;
    
    private UniformTable? _boundUniforms;

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


    internal GlGraphicsContext(GL gl, GraphicsConfiguration configuration, GraphicsResourceStore store, in RenderFrameContext initialFrameCtx)
    {
        _gl = gl;
        Configuration = configuration;
        _store = store;

        _boundTextures = new ushort[configuration.MaxTextureImageUnits];

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
        UseShader(0);
        BindMesh(0);
        BindVertexBuffer(0);
        BindIndexBuffer(0);
        for (uint i = 0; i < _boundTextures.Length; i++)
        {
            BindTexture(0, i);
        }
    }

    public void Clear(Color color)
    {
        _gl.ClearColor(color);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void UseShader(ushort resourceId)
    {
        if (_boundShader == resourceId) return;

        if (resourceId == 0)
        {
            _boundShader = 0;
            _boundUniforms = null;
            _gl.UseProgram(0);
            return;
        }
        
        var resource = _store.Get<GlShader>(resourceId);
        var uniformTable = _store.GetUniformTable(resourceId);

        _gl.UseProgram(resource!.Handle);
        _boundShader = resourceId;
        _boundUniforms = uniformTable;
    }

    public void BindTexture(ushort resourceId, uint slot)
    {
        if (slot >= Configuration.MaxTextureImageUnits)
             GraphicsException.ThrowCapabilityExceeded<GlTexture2D>("Texture slot", (int)slot,
                Configuration.MaxTextureImageUnits);

        if (_boundShader == resourceId) return;

        if (resourceId == 0)
        {
            _gl.BindTextureUnit(slot, 0);
            _boundTextures[slot] = 0;
            return;
        }

        if (_boundTextures[slot] == resourceId) return;

        var resource = _store.Get<GlTexture2D>(resourceId);

        _gl.BindTextureUnit(slot, resource!.Handle);
        _boundTextures[slot] = resourceId;
    }

    public void BindMesh(ushort resourceId)
    {
        if (_boundShader == resourceId) return;

        if (resourceId == 0)
        {
            _gl.BindVertexArray(0);
            _boundVao = 0;
            return;
        }
        
        var resource = _store.Get<GlMesh>(resourceId);
        _gl.BindVertexArray(resource!.Handle);
        _boundVao = resourceId;
    }

    public void BindVertexBuffer(ushort resourceId)
    {
        if (_boundShader == resourceId) return;

        if (resourceId == 0)
        {
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _boundVertexBuffer = 0;
            return;
        }

        var resource = _store.Get<GlVertexBuffer>(resourceId)!;
        _gl.BindBuffer(resource.GlBufferTarget, resource.Handle);
        _boundVertexBuffer = resourceId;
    }

    public void BindIndexBuffer(ushort resourceId)
    {
        if (_boundShader == resourceId) return;

        if (resourceId == 0)
        {
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
            _boundIndexBuffer = 0;
            return;
        }

        var resource = _store.Get<GlIndexBuffer>(resourceId)!;
        _gl.BindBuffer(resource.GlBufferTarget, resource.Handle);
        _boundIndexBuffer = resourceId;
    }

    public void SetVertexBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        SetBufferData<GlVertexBuffer, T>(_boundVertexBuffer, data);
    }

    public void SetIndexBuffer(ReadOnlySpan<uint> data)
    {
        SetBufferData<GlIndexBuffer, uint>(_boundIndexBuffer, data);
    }

    private void SetBufferData<TBuffer, TData>(ushort resourceId, ReadOnlySpan<TData> data) 
        where TBuffer : GlBuffer where TData : unmanaged
    {
        var buffer = _store.Get<TBuffer>(resourceId);
        ValidateBoundResource(buffer);
        
        if (buffer!.IsStatic && buffer.BufferSizeInBytes > 0)
             GraphicsException.ThrowInvalidBufferData<GlBuffer>(nameof(buffer), "Buffer is static");

        buffer.ElementCount = data.Length;
        buffer.ElementSize = Unsafe.SizeOf<TData>();
        Gl.BufferData(buffer.GlBufferTarget, (nuint)buffer.BufferSizeInBytes, data, buffer.GlBufferUsage);

        CheckGlError();
    }

    public void UploadVertexBuffer<T>(ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        UploadBufferData<GlVertexBuffer,T>(_boundVertexBuffer, data, offsetElements);
    }

    public void UploadIndexBuffer(ReadOnlySpan<uint> data, int offsetElements)
    {
        UploadBufferData<GlIndexBuffer, uint>(_boundIndexBuffer, data, offsetElements);
    }

    private void UploadBufferData<TBuffer, TData>(ushort resourceId, ReadOnlySpan<TData> data, int offsetElements) 
        where TBuffer : GlBuffer where TData : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offsetElements, nameof(offsetElements));
        var buffer = _store.Get<TBuffer>(resourceId);
        ValidateBoundResource(buffer);

        if (buffer.IsStatic)
             GraphicsException.ThrowInvalidBufferData<GlBuffer>(nameof(buffer), "Buffer is static");

        var elementSize = Unsafe.SizeOf<TData>();

        if (elementSize != buffer.ElementSize)
             GraphicsException.ThrowInvalidBufferData<GlBuffer>(nameof(elementSize), "Invalid element size");

        if (data.Length + offsetElements > buffer.ElementCount)
        {
             GraphicsException.ThrowInvalidBufferData<GlBuffer>(null,
                $"Upload data {data.Length + offsetElements} cannot be bigger than {buffer.ElementCount} elements.");
        }


        nint byteOffset = (nint)((long)offsetElements * elementSize);
        Gl.BufferSubData(buffer.GlBufferTarget, byteOffset, data);

        CheckGlError(); // throws here
    }

    public void Draw(uint drawCount = 0)
    {
        var mesh = _store.Get<GlMesh>(_boundVao);
        ValidateBoundResource(mesh);

        if(mesh.VertexBufferId == 0) 
            GraphicsException.ThrowInvalidState($"Mesh is missing VertexBuffer");

        var count = drawCount > 0 ? drawCount : mesh.DrawCount;
        _gl.DrawArrays(PrimitiveType.Triangles, 0, count);
        _drawTriangleCount += (int)count;
        _drawCallCount++;
    }

    public unsafe void DrawIndexed(uint drawCount = 0)
    {
        var mesh = _store.Get<GlMesh>(_boundVao);
        ValidateBoundResource(mesh);
        if(mesh.IndexBufferId == 0) 
            GraphicsException.ThrowInvalidState($"Mesh is missing IndexBuffer"); 

        var count = drawCount > 0 ? drawCount : mesh.DrawCount;

        _gl.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedInt, (void*)0);
        _drawTriangleCount += (int)count;
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
    public void SetUniforma(ShaderUniform uniform, int value) => _gl.Uniform1(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, int value) => _gl.Uniform1(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, uint value) => _gl.Uniform1(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, float value) => _gl.Uniform1(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector2D<float> value) =>
        _gl.Uniform2(_boundUniforms![uniform], value.X, value.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector3D<float> value)
    {
        _gl.Uniform3(_boundUniforms![uniform], value.X, value.Y, value.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector4D<float> value)
    {
        _gl.Uniform4(_boundUniforms![uniform], value.X, value.Y, value.Z, value.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetUniform(ShaderUniform uniform, in Matrix4X4<float> value)
    {
        var p = (float*)Unsafe.AsPointer(ref Unsafe.AsRef(in value));
        _gl.UniformMatrix4(_boundUniforms![uniform], 1, false, p);
    }


    private void CheckGlError()
    {
        var error = Gl.GetError();
        if (error != (GLEnum)ErrorCode.NoError)
            throw new OpenGlException(error);
    }

    private static void ValidateBoundResource<T>([NotNull]T? resource) where T : IGraphicsResource
    {
        if (resource is null)
            throw GraphicsException.ResourceIsNull<T>(nameof(resource));
        
        if (resource.IsDisposed)
            throw GraphicsException.ResourceIsDisposed<T>(nameof(resource));
    }
}