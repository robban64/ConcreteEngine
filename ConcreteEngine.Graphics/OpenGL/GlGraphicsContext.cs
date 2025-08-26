#region

using System.Diagnostics;
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
    private readonly int _glMinor = 0;
    private readonly int _glMajor = 0;

    private readonly GlContextBindingView _store;
    private readonly UniformRegistry _uniformRegistry;

    private BlendMode _blendMode = BlendMode.Alpha;
    private bool _depthTest = true;

    private uint _currentDrawFboHandle = 0; // 0 = screen
    private uint _currentReadFboHandle = 0; // 0 = screen

    private FrameBufferId _boundFboId = new(0);
    private FrameBufferId _boundReadFboId = new(0);

    private ShaderId _boundShaderId = new(0);
    private VertexBufferId _boundVertexBufferId = new(0);
    private IndexBufferId _boundIndexBufferId = new(0);
    private MeshId _boundVaoId = new(0);
    private readonly TextureId[] _boundTextures;

    private UniformTable? _boundUniforms;

    private Vector2D<int> _viewport;
    private Vector2D<int> _currentViewport;

    private float _deltaTime = 0f;
    private int _drawTriangleCount = 0;
    private int _drawCallCount = 0;

    public GraphicsConfiguration Configuration { get; }
    public BlendMode BlendMode => _blendMode;
    public bool DepthTest => _depthTest;
    public Vector2D<int> ViewportSize => _viewport;

    public GL Gl => _gl;


    internal GlGraphicsContext(
        GL gl,
        GraphicsConfiguration configuration,
        GlContextBindingView store,
        UniformRegistry uniformRegistry,
        in GraphicsFrameContext initialFrameCtx)
    {
        _gl = gl;
        Configuration = configuration;
        _store = store;
        _uniformRegistry = uniformRegistry;

        _boundTextures = new TextureId[configuration.MaxTextureImageUnits];

        _viewport = initialFrameCtx.ViewportSize;
        _currentViewport = initialFrameCtx.ViewportSize;


        _gl.GetInteger(GetPName.MajorVersion, out _glMajor);
        _gl.GetInteger(GetPName.MinorVersion, out _glMinor);
        int glVersion = _glMajor * 100 + _glMinor * 10;

        _gl.Disable(GLEnum.CullFace);
        _gl.Enable(GLEnum.Dither);
        _gl.Enable(GLEnum.Multisample);
        _gl.PixelStore(GLEnum.UnpackAlignment, 1);
        //_gl.Enable(EnableCap.FramebufferSrgb);

        //_gl.Disable(GLEnum.DepthTest);
        //_gl.DepthMask(false);
    }

    public void BeginFrame(in GraphicsFrameContext frameCtx)
    {
        _blendMode = BlendMode.None;
        _depthTest = true;

        _deltaTime = frameCtx.DeltaTime;
        _viewport = frameCtx.ViewportSize;
        _currentViewport = frameCtx.ViewportSize;

        _drawCallCount = 0;
        _drawTriangleCount = 0;

        SetBlendMode(BlendMode.None);
        SetDepthTest(true);
        Clear(Color.CornflowerBlue, ClearBufferFlag.ColorAndDepth);
    }

    public void EndFrame()
    {
        // unbind context
        BindMesh(default);
        BindVertexBuffer(default);
        BindIndexBuffer(default);
        BindFramebuffer(default);
        UseShader(default);
        for (uint i = 0; i < _boundTextures.Length; i++)
        {
            BindTexture(default, i);
        }
    }

    public void Clear(Color color, ClearBufferFlag flags)
    {
        _gl.ClearColor(color);
        _gl.Clear(flags.ToGlEnum());
    }


    public void BeginScreenPass(Color? clear, ClearBufferFlag? flags)
    {
        if (_boundFboId != default) GraphicsException.ThrowInvalidState("Cannot begin screen pass while FBO is bound.");

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Viewport(_viewport);
        if (clear.HasValue && flags.HasValue) Clear(clear.Value, flags.Value);

        _currentDrawFboHandle = 0;
        _currentReadFboHandle = 0;

        _boundFboId = default;
        _boundReadFboId = default;
        _currentViewport = _viewport;
    }

    public void BeginRenderPass(FrameBufferId fboId, Color? clear, ClearBufferFlag? flags)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fboId.Id, nameof(fboId));
        if (_boundFboId == fboId) GraphicsException.ThrowInvalidState($"FBO is {fboId} already bound.");

        ref readonly var meta = ref _store.FboStore.GetMeta(fboId);
        var handle = _store.FboStore.GetHandle(fboId).Handle;

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
        _gl.Viewport(meta.Size);
        if (clear.HasValue && flags.HasValue) Clear(clear.Value, flags.Value);

        _currentDrawFboHandle = handle;
        _currentReadFboHandle = handle;

        _boundFboId = fboId;
        _boundReadFboId = fboId;
        _currentViewport = meta.Size;
    }

    public void EndRenderPass()
    {
        if (_boundFboId == default) GraphicsException.ResourceNotBound<GlFrameBufferHandle>(nameof(_boundFboId));

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Viewport(_viewport);

        _currentDrawFboHandle = 0;
        _currentReadFboHandle = 0;

        _boundFboId = default;
        _boundReadFboId = default;
        _currentViewport = _viewport;
    }

    public void BlitFramebuffer(FrameBufferId fromId, FrameBufferId toId = default, bool linearFilter = true)
    {
        ref readonly var fromFbo = ref _store.FboStore.GetMeta(fromId);
        var fromHandle = _store.FboStore.GetHandle(fromId).Handle;

        var srcSize = fromFbo.Size;

        uint toHandle = 0;
        var dstSize = _viewport;
        if (toId != default)
        {
            ref readonly var toFbo = ref _store.FboStore.GetMeta(toId);
            toHandle = _store.FboStore.GetHandle(toId).Handle;
            dstSize = toFbo.Size;
        }

        Debug.Assert(toHandle != fromHandle, "READ and DRAW FBO must differ for resolve.");

        // Save current state …
        var prevReadFbo = _currentReadFboHandle;
        var prevDrawFbo = _currentDrawFboHandle;
        var prevViewport = _currentViewport;


        // If source is MSAA, filter must be NEAREST for the resolve.
        var filter = fromFbo.Msaa
            ? BlitFramebufferFilter.Nearest
            : linearFilter
                ? BlitFramebufferFilter.Linear
                : BlitFramebufferFilter.Nearest;

        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fromHandle);
        _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, toHandle);

        _gl.BlitFramebuffer(
            0, 0, srcSize.X, srcSize.Y,
            0, 0, dstSize.X, dstSize.Y,
            ClearBufferMask.ColorBufferBit,
            filter
        );

        // Restore previous bindings & viewport …
        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, prevReadFbo);
        _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, prevDrawFbo);
        _gl.Viewport(prevViewport);

        _currentReadFboHandle = prevReadFbo;
        _currentDrawFboHandle = prevDrawFbo;
        _currentViewport = prevViewport;
        // _boundFboId stays whatever it was (0 for screen or the FBO id if inside a pass)
    }

    public void SetBlendMode(BlendMode blendMode)
    {
        if (_blendMode != BlendMode.Unset && _blendMode == blendMode) return;

        _blendMode = blendMode;

        switch (blendMode)
        {
            case BlendMode.Alpha:
                _gl.Enable(EnableCap.Blend);
                _gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
                _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
            case BlendMode.PremultipliedAlpha:
                _gl.Enable(EnableCap.Blend);
                _gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
                _gl.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
                break;
            case BlendMode.Additive:
                _gl.Enable(EnableCap.Blend);
                _gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
                _gl.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                break;
            case BlendMode.None:
                _gl.Disable(EnableCap.Blend);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(blendMode), blendMode, null);
        }
    }

    public void SetDepthTest(bool depthTest)
    {
        if (_depthTest == depthTest) return;
        if (depthTest) _gl.Enable(EnableCap.DepthTest);
        else _gl.Disable(EnableCap.DepthTest);
    }


    public void BindFramebuffer(FrameBufferId id)
    {
        if (_boundFboId == id) return;
        if (id == default)
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _boundFboId = default;
            return;
        }

        var handle = _store.FboStore.GetHandle(id).Handle;

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
        _boundFboId = id;
    }

    public void BindTexture(TextureId id, uint slot)
    {
        if (slot >= Configuration.MaxTextureImageUnits)
            GraphicsException.ThrowCapabilityExceeded<TextureId>("Texture slot", (int)slot,
                Configuration.MaxTextureImageUnits);

        if (_boundTextures[slot] == id) return;

        if (id == default)
        {
            _gl.BindTextureUnit(slot, 0);
            _boundTextures[slot] = default;
            return;
        }

        if (_boundTextures[slot] == id) return;

        var handle = _store.TextureStore.GetHandle(id).Handle;

        _gl.BindTextureUnit(slot, handle);
        _boundTextures[slot] = id;
    }

    public void BindMesh(MeshId id)
    {
        if (_boundVaoId == id) return;

        if (id == default)
        {
            _gl.BindVertexArray(0);
            _boundVaoId = default;
            return;
        }

        var handle = _store.MeshStore.GetHandle(id).Handle;
        _gl.BindVertexArray(handle);
        _boundVaoId = id;
    }

    public void BindVertexBuffer(VertexBufferId id)
    {
        if (_boundVertexBufferId == id) return;

        if (id == default)
        {
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _boundVertexBufferId = default;
            return;
        }

        var handle = _store.VboStore.GetHandle(id).Handle;
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, handle);
        _boundVertexBufferId = id;
    }

    public void BindIndexBuffer(IndexBufferId id)
    {
        if (_boundIndexBufferId == id) return;

        if (id == default)
        {
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
            _boundIndexBufferId = default;
            return;
        }

        var handle = _store.IboStore.GetHandle(id).Handle;
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, handle);
        _boundIndexBufferId = id;
    }

    public void SetVertexBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        ref readonly var meta = ref _store.VboStore.GetMeta(_boundVertexBufferId);
        var handle = _store.VboStore.GetHandle(_boundVertexBufferId);
        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.ElementSize > 0)
            GraphicsException.ThrowInvalidBufferData<GlVertexBufferHandle>(_boundVertexBufferId.ToString(),
                "Buffer is static");

        var elementCount = data.Length;
        var elementSize = Unsafe.SizeOf<T>();
        var size = elementSize * elementCount;


        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)size, data, meta.Usage.ToGlEnum());
        CheckGlError();

        var newMeta = new VertexBufferMeta(meta.Usage, (uint)elementCount, (uint)elementSize);
        _store.VboStore.Replace(_boundVertexBufferId, in newMeta, handle, out _);
    }

    public void SetIndexBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        ref readonly var meta = ref _store.IboStore.GetMeta(_boundIndexBufferId);
        var handle = _store.IboStore.GetHandle(_boundIndexBufferId);

        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.ElementSize > 0)
            GraphicsException.ThrowInvalidBufferData<GlIndexBufferHandle>(_boundIndexBufferId.ToString(),
                "Buffer is static");

        var elementCount = data.Length;
        var elementSize = Unsafe.SizeOf<T>();
        var size = elementSize * elementCount;

        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)size, data, meta.Usage.ToGlEnum());
        CheckGlError();

        var newMeta = new IndexBufferMeta(meta.Usage, (uint)elementCount, (uint)elementSize);
        _store.IboStore.Replace(_boundIndexBufferId, in newMeta, handle, out _);
    }

    public void UploadVertexBuffer<T>(ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        ref readonly var meta = ref _store.VboStore.GetMeta(_boundVertexBufferId);
        var handle = _store.VboStore.GetHandle(_boundVertexBufferId);
        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.ElementSize > 0)
            GraphicsException.ThrowInvalidBufferData<GlVertexBufferHandle>(_boundVertexBufferId.ToString(),
                "Buffer is static");

        var elementSize = Unsafe.SizeOf<T>();

        if (elementSize != meta.ElementSize)
            GraphicsException.ThrowInvalidBufferData<GlVertexBufferHandle>(nameof(elementSize), "Invalid element size");

        if (data.Length + offsetElements > meta.ElementCount)
        {
            GraphicsException.ThrowInvalidBufferData<GlVertexBufferHandle>(null,
                $"Upload data {data.Length + offsetElements} cannot be bigger than {meta.ElementCount} elements.");
        }

        nint byteOffset = (nint)((long)offsetElements * elementSize);
        _gl.BufferSubData(BufferTargetARB.ArrayBuffer, byteOffset, data);

        CheckGlError(); // throws here
    }

    public void UploadIndexBuffer<T>(ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        ref readonly var meta = ref _store.IboStore.GetMeta(_boundIndexBufferId);
        var handle = _store.IboStore.GetHandle(_boundIndexBufferId);
        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.ElementSize > 0)
            GraphicsException.ThrowInvalidBufferData<GlVertexBufferHandle>(_boundIndexBufferId.ToString(),
                "Buffer is static");

        var elementSize = Unsafe.SizeOf<T>();

        if (elementSize != meta.ElementSize)
            GraphicsException.ThrowInvalidBufferData<GlVertexBufferHandle>(nameof(elementSize), "Invalid element size");

        if (data.Length + offsetElements > meta.ElementCount)
        {
            GraphicsException.ThrowInvalidBufferData<GlVertexBufferHandle>(null,
                $"Upload data {data.Length + offsetElements} cannot be bigger than {meta.ElementCount} elements.");
        }

        nint byteOffset = (nint)((long)offsetElements * elementSize);
        _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, byteOffset, data);

        CheckGlError(); // throws here
    }

    public void DrawMesh(uint drawCount = 0)
    {
        ref readonly var meta = ref _store.MeshStore.GetMeta(_boundVaoId);

        if (meta.VertexBufferId == default)
            GraphicsException.ThrowInvalidState($"Mesh is missing VertexBuffer");

        var count = drawCount > 0 ? drawCount : meta.DrawCount;

        if (meta.ElementType == IboElementType.Invalid)
            DrawArrays(in meta, count);
        else
            DrawElements(in meta, count);

        _drawTriangleCount += (int)count;
        _drawCallCount++;
    }


    private void DrawArrays(in MeshMeta meta, uint drawCount)
    {
        _gl.DrawArrays(meta.Primitive.ToGlEnum(), 0, drawCount);
    }

    public unsafe void DrawElements(in MeshMeta meta, uint drawCount)
    {
        _gl.DrawElements(meta.Primitive.ToGlEnum(), drawCount, meta.ElementType.ToGlEnum(), (void*)0);
    }


    public void UseShader(ShaderId id)
    {
        if (_boundShaderId == id) return;

        if (id == default)
        {
            _boundShaderId = default;
            _boundUniforms = null;
            _gl.UseProgram(0);
            return;
        }

        var handle = _store.ShaderStore.GetHandle(id);
        var uniformTable = _uniformRegistry.Get(id);

        _gl.UseProgram(handle.Handle);
        _boundShaderId = id;
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
        var error = _gl.GetError();
        if (error != (GLEnum)ErrorCode.NoError)
            throw new OpenGlException(error);
    }
}