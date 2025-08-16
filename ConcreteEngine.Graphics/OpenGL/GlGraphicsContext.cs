#region

using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
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

public sealed class GlGraphicsContext : IGraphicsContext
{
    private readonly GL _gl;
    private readonly int glMinor = 0;
    private readonly int glMajor = 0;

    private readonly GraphicsResourceStore _store;
    private readonly UniformRegistry _uniformRegistry;

    private BlendMode _blendMode = BlendMode.Alpha;

    private ushort _boundFboId = 0;
    private ushort _boundShaderId = 0;
    private ushort _boundVertexBufferId = 0;
    private ushort _boundIndexBufferId = 0;
    private ushort _boundVaoId = 0;
    private readonly ushort[] _boundTextures;

    private UniformTable? _boundUniforms;

    private Vector2D<int> _viewportSize;

    private float _deltaTime = 0f;
    private int _drawTriangleCount = 0;
    private int _drawCallCount = 0;

    public GraphicsConfiguration Configuration { get; }
    public BlendMode BlendMode => _blendMode;
    public Vector2D<int> ViewportSize => _viewportSize;

    public GL Gl => _gl;


    internal GlGraphicsContext(
        GL gl,
        GraphicsConfiguration configuration,
        GraphicsResourceStore store,
        UniformRegistry  uniformRegistry,
        in GraphicsFrameContext initialFrameCtx)
    {
        _gl = gl;
        Configuration = configuration;
        _store = store;
        _uniformRegistry = uniformRegistry;

        _boundTextures = new ushort[configuration.MaxTextureImageUnits];

        _viewportSize = initialFrameCtx.ViewportSize;


        gl.GetInteger(GetPName.MajorVersion, out glMajor);
        gl.GetInteger(GetPName.MinorVersion, out glMinor);
        int glVersion = glMajor * 100 + glMinor * 10;

        _gl.Disable(GLEnum.CullFace);
        _gl.Disable(GLEnum.DepthTest);
        _gl.Disable(GLEnum.Dither);

        _gl.Enable(GLEnum.Multisample);

        _gl.PixelStore(GLEnum.UnpackAlignment, 1);

        _gl.DepthMask(false);
    }

    public void BeginFrame(in GraphicsFrameContext frameCtx)
    {
        _blendMode = BlendMode.Unset;

        _deltaTime = frameCtx.DeltaTime;
        _viewportSize = frameCtx.ViewportSize;

        _drawCallCount = 0;
        _drawTriangleCount = 0;
    }

    public void EndFrame()
    {
        // unbind context
        BindMesh(0);
        BindVertexBuffer(0);
        BindIndexBuffer(0);
        BindFramebuffer(0);
        UseShader(0);
        for (uint i = 0; i < _boundTextures.Length; i++)
        {
            BindTexture(0, i);
        }
    }

    public void Clear(Color color, ClearBufferFlag flags)
    {
        _gl.ClearColor(color);
        _gl.Clear(flags.ToGlEnum());
    }


    public void BeginScreenPass(Color? clear, ClearBufferFlag flags = ClearBufferFlag.Color)
    {
        if (_boundFboId != 0) GraphicsException.ThrowInvalidState("Cannot begin screen pass while FBO is bound.");
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Viewport(_viewportSize);
        if (clear.HasValue) Clear(clear.Value, flags);
    }

    public void BeginRenderPass(ushort fboId, Color? clear, ClearBufferFlag flags = ClearBufferFlag.Color)
    {
        ArgumentOutOfRangeException.ThrowIfZero(fboId, nameof(fboId));
        if (_boundFboId == fboId) GraphicsException.ThrowInvalidState($"FBO is {fboId} already bound.");

        var fbo = _store.Get<GlFramebuffer>(fboId);
        ValidateResource(fbo);

        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo.Handle);
        _gl.Viewport(_viewportSize);
        if (clear.HasValue) Clear(clear.Value, flags);
        _boundFboId = fboId;
    }

    public void EndRenderPass()
    {
        if (_boundFboId == 0) GraphicsException.ResourceNotBound<GlFramebuffer>(nameof(_boundFboId));

        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        Gl.Viewport(_viewportSize);
        _boundFboId = 0;
    }

    public void BlitFramebufferTo(ushort fromId, ushort toId = 0, Vector2D<int>? size = null,
        bool linearFilter = true)
    {
        var fromFbo = _store.Get<GlFramebuffer>(fromId);
        ValidateResource(fromFbo);

        uint toFboHandle = 0;
        var toFboSize = size ?? _viewportSize;

        if (toId > 0)
        {
            var toFbo = _store.Get<GlFramebuffer>(toId);
            ValidateResource(toFbo);
            toFboHandle = toFbo.Handle;
        }

        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fromFbo.Handle);
        _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, toFboHandle);

        var (sx, sy, dx, dy) = (fromFbo.Size.X, fromFbo.Size.Y, toFboSize.X, toFboSize.Y);
        _gl.BlitFramebuffer(
            0, 0, sx, sy,
            0, 0, dx, dy,
            ClearBufferMask.ColorBufferBit,
            linearFilter ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest
        );

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Viewport(_viewportSize);
    }

    public void SetBlendMode(BlendMode blendMode)
    {
        if (_blendMode != BlendMode.Unset && _blendMode == blendMode) return;

        _blendMode = blendMode;

        switch (blendMode)
        {
            case BlendMode.Alpha:
                _gl.Enable(EnableCap.Blend);
                _gl.BlendEquation(GLEnum.FuncAdd);
                _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
            case BlendMode.PremultipliedAlpha:
                _gl.Enable(EnableCap.Blend);
                _gl.BlendEquation(GLEnum.FuncAdd);
                _gl.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
                break;
            case BlendMode.Additive:
                _gl.Enable(EnableCap.Blend);
                _gl.BlendEquation(GLEnum.FuncAdd);
                _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                break;
            case BlendMode.None:
                _gl.Disable(EnableCap.Blend);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(blendMode), blendMode, null);
        }
    }

    public void BindFramebufferTexture(ushort framebufferId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(framebufferId, 0);
        var frameBuffer = _store.Get<GlFramebuffer>(framebufferId);
        ValidateResource(frameBuffer);
        BindTexture(frameBuffer.ColorTextureId, 0);
    }

    public void BindFramebuffer(ushort resourceId)
    {
        if (_boundFboId == resourceId) return;
        if (resourceId == 0)
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(_viewportSize);
            Gl.Clear((uint)ClearBufferMask.ColorBufferBit);
            _boundFboId = 0;
            return;
        }

        var resource = _store.Get<GlFramebuffer>(resourceId);
        ValidateResource(resource);

        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, resource.Handle);
        _boundFboId = resourceId;
    }

    public void BindTexture(ushort resourceId, uint slot)
    {
        if (slot >= Configuration.MaxTextureImageUnits)
            GraphicsException.ThrowCapabilityExceeded<GlTexture2D>("Texture slot", (int)slot,
                Configuration.MaxTextureImageUnits);

        if (_boundShaderId == resourceId) return;

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
        if (_boundShaderId == resourceId) return;

        if (resourceId == 0)
        {
            _gl.BindVertexArray(0);
            _boundVaoId = 0;
            return;
        }

        var resource = _store.Get<GlMesh>(resourceId);
        _gl.BindVertexArray(resource!.Handle);
        _boundVaoId = resourceId;
    }

    public void BindVertexBuffer(ushort resourceId)
    {
        if (_boundShaderId == resourceId) return;

        if (resourceId == 0)
        {
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _boundVertexBufferId = 0;
            return;
        }

        var resource = _store.Get<GlVertexBuffer>(resourceId)!;
        _gl.BindBuffer(resource.GlBufferTarget, resource.Handle);
        _boundVertexBufferId = resourceId;
    }

    public void BindIndexBuffer(ushort resourceId)
    {
        if (_boundShaderId == resourceId) return;

        if (resourceId == 0)
        {
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
            _boundIndexBufferId = 0;
            return;
        }

        var resource = _store.Get<GlIndexBuffer>(resourceId)!;
        _gl.BindBuffer(resource.GlBufferTarget, resource.Handle);
        _boundIndexBufferId = resourceId;
    }

    public void SetVertexBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        SetBufferData<GlVertexBuffer, T>(_boundVertexBufferId, data);
    }

    public void SetIndexBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        SetBufferData<GlIndexBuffer, T>(_boundIndexBufferId, data);
    }

    private void SetBufferData<TBuffer, TData>(ushort resourceId, ReadOnlySpan<TData> data)
        where TBuffer : GlBuffer where TData : unmanaged
    {
        var buffer = _store.Get<TBuffer>(resourceId);
        ValidateResource(buffer);

        if (buffer!.IsStatic && buffer.BufferSizeInBytes > 0)
            GraphicsException.ThrowInvalidBufferData<GlBuffer>(nameof(buffer), "Buffer is static");

        buffer.ElementCount = data.Length;
        buffer.ElementSize = Unsafe.SizeOf<TData>();
        Gl.BufferData(buffer.GlBufferTarget, (nuint)buffer.BufferSizeInBytes, data, buffer.GlBufferUsage);

        CheckGlError();
    }

    public void UploadVertexBuffer<T>(ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        UploadBufferData<GlVertexBuffer, T>(_boundVertexBufferId, data, offsetElements);
    }

    public void UploadIndexBuffer<T>(ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        UploadBufferData<GlIndexBuffer, T>(_boundIndexBufferId, data, offsetElements);
    }

    private void UploadBufferData<TBuffer, TData>(ushort resourceId, ReadOnlySpan<TData> data, int offsetElements)
        where TBuffer : GlBuffer where TData : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offsetElements, nameof(offsetElements));
        var buffer = _store.Get<TBuffer>(resourceId);
        ValidateResource(buffer);

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
        var mesh = _store.Get<GlMesh>(_boundVaoId);
        ValidateResource(mesh);

        if (mesh.VertexBufferId == 0)
            GraphicsException.ThrowInvalidState($"Mesh is missing VertexBuffer");
        if (mesh.IndexBufferId > 0)
            GraphicsException.ThrowInvalidState(
                $"Elemental mesh must use {nameof(DrawIndexed)} instead of {nameof(Draw)}");

        var count = drawCount > 0 ? drawCount : mesh.DrawCount;
        _gl.DrawArrays(mesh.PrimitiveType, 0, count);
        _drawTriangleCount += (int)count;
        _drawCallCount++;
    }

    public unsafe void DrawIndexed(uint drawCount = 0)
    {
        var mesh = _store.Get<GlMesh>(_boundVaoId);
        ValidateResource(mesh);
        if (mesh.IndexBufferId == 0)
            GraphicsException.ThrowInvalidState($"Mesh is missing IndexBuffer");

        var count = drawCount > 0 ? drawCount : mesh.DrawCount;

        _gl.DrawElements(PrimitiveType.Triangles, count, mesh.ElementType, (void*)0);
        _drawTriangleCount += (int)count;
        _drawCallCount++;
    }


    public void UseShader(ushort resourceId)
    {
        if (_boundShaderId == resourceId) return;

        if (resourceId == 0)
        {
            _boundShaderId = 0;
            _boundUniforms = null;
            _gl.UseProgram(0);
            return;
        }

        var resource = _store.Get<GlShader>(resourceId);
        var uniformTable = _uniformRegistry.Get(resourceId);

        _gl.UseProgram(resource!.Handle);
        _boundShaderId = resourceId;
        _boundUniforms = uniformTable;
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
    public void SetUniform(ShaderUniform uniform, Vector2 value) =>
        _gl.Uniform2(_boundUniforms![uniform], value.X, value.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector3 value)
    {
        _gl.Uniform3(_boundUniforms![uniform], value.X, value.Y, value.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector4 value)
    {
        _gl.Uniform4(_boundUniforms![uniform], value.X, value.Y, value.Z, value.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetUniform(ShaderUniform uniform, in Matrix4x4 value)
    {
        //_gl.UniformMatrix4(_boundUniforms![uniform], 1, false, (float*) &value);
        var p = (float*)Unsafe.AsPointer(ref Unsafe.AsRef(in value));
        _gl.UniformMatrix4(_boundUniforms![uniform], 1, false, p);
    }


    private void CheckGlError()
    {
        var error = Gl.GetError();
        if (error != (GLEnum)ErrorCode.NoError)
            throw new OpenGlException(error);
    }

    private static void ValidateResource<T>([NotNull] T? resource) where T : IGraphicsResource
    {
        if (resource is null)
            throw GraphicsException.ResourceIsNull<T>(nameof(resource));

        if (resource.IsDisposed)
            throw GraphicsException.ResourceIsDisposed<T>(nameof(resource));
    }
}