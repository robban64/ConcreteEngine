using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlBackendDriver : IGraphicsDriver
{
    private GL _gl = null!;
    private int _glMinor = 0;
    private int _glMajor = 0;

    private DeviceCapabilities _capabilities = null!;
    private GraphicsConfiguration _configuration = null!;

    private GlTextureFactory _textureFactory = null!;
    private GlShaderFactory _shaderFactory = null!;
    private GlFboFactory _fboFactory = null!;

    public GraphicsConfiguration Configuration => _configuration;

    public DeviceCapabilities Capabilities => _capabilities;


    private readonly OpenGlResourceStores _store;

    private static DebugProc _cb;

    void EnableGlDebug(GL gl)
    {
        unsafe
        {
            _cb = (src, type, id, severity, len, msg, user) =>
            {
                var text = SilkMarshal.PtrToString((nint)msg);
                Console.WriteLine($"[GL {severity}] {type} {id}: {text}");
            };

            gl.Enable(EnableCap.DebugOutput);
            gl.Enable(EnableCap.DebugOutputSynchronous);
            gl.DebugMessageCallback(_cb, null);
            gl.DebugMessageControl(GLEnum.DontCare, GLEnum.DontCare, GLEnum.DebugSeverityNotification,
                0, null, false);


            gl.Enable(EnableCap.DebugOutput);
            gl.Enable(EnableCap.DebugOutputSynchronous);
            gl.DebugMessageCallback(_cb, null);
            gl.DebugMessageControl(GLEnum.DontCare, GLEnum.DontCare, GLEnum.DebugSeverityNotification, 0, null, false);
        }
    }


    internal GlBackendDriver()
    {
        _store = new OpenGlResourceStores();
    }

    internal void Initialize(GlStartupConfig config)
    {
        unsafe
        {
            _gl = config.DriverContext;
            _capabilities = CreateDeviceCapabilities(_gl);
            _configuration = new GraphicsConfiguration();


            Console.WriteLine($"OpenGL version {Capabilities.GlVersion} loaded.");
            Console.WriteLine("--Device Capability--");
            Console.WriteLine(Capabilities.ToString());


            _gl.GetInteger(GetPName.MajorVersion, out _glMajor);
            _gl.GetInteger(GetPName.MinorVersion, out _glMinor);
            int glVersion = _glMajor * 100 + _glMinor * 10;

            _gl.Enable(GLEnum.Dither);
            _gl.Enable(GLEnum.Multisample);
            _gl.Enable(EnableCap.TextureCubeMapSeamless);
            _gl.PixelStore(GLEnum.UnpackAlignment, 1);
        
            _gl.DepthMask(true);
        
            _gl.Enable(EnableCap.CullFace);
            _gl.CullFace(TriangleFace.Back);
            _gl.FrontFace(FrontFaceDirection.Ccw);

        
            EnableGlDebug(_gl);

            _textureFactory = new GlTextureFactory();
            _shaderFactory = new GlShaderFactory();
            _fboFactory = new GlFboFactory(_textureFactory);

            _textureFactory.AttachGlContext(_gl, Capabilities);
            _shaderFactory.AttachGlContext(_gl, Capabilities);
            _fboFactory.AttachGlContext(_gl, Capabilities);
        }
    }

    public void PrepareFrame()
    {
    }

    public void ValidateEndFrame()
    {
        CheckGlError();
    }

    public void Clear(Color4 color, ClearBufferFlag flags)
    {
        _gl.ClearColor(color.R, color.G, color.B, 1);
        _gl.Clear(flags.ToGlEnum());
        CheckGlError();
    }

    public void SetBlendMode(BlendMode blendMode)
    {
        var (enabled, eq, src, dst) = blendMode.ToGlEnum();
        if (enabled)
        {
            _gl.Enable(EnableCap.Blend);
            _gl.BlendEquation(eq);
            _gl.BlendFunc(src, dst);
        }
        else
        {
            _gl.Disable(EnableCap.Blend);
        }
    }

    public void SetDepthMode(DepthMode depthMode)
    {
        if (depthMode == DepthMode.WriteLequal)
        {
            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthFunc(DepthFunction.Lequal);
        }
        else _gl.Disable(EnableCap.DepthTest);
        return;
        var (cap, func, mask) = depthMode.ToGlEnum();
        _gl.Enable(cap);
        _gl.DepthFunc(func);
        _gl.DepthMask(mask);
    }

    public void SetCullMode(CullMode cullMode)
    {
        var (cap, face, front) = cullMode.ToGlEnum();
        _gl.Enable(cap);
        _gl.CullFace(face);
        _gl.FrontFace(front);
    }

    public void SetViewport(in Vector2D<int> viewport) => _gl.Viewport(viewport);


    public GfxHandle CreateUniformBuffer(UniformGpuSlot slot, UboDefaultCapacity capacity, uint blockSize,
        out UniformBufferMeta meta)
    {
        var handle = _shaderFactory.CreateUniformBuffer(slot, capacity, blockSize, out meta);
        CheckGlError();

        return _store.UboStore.Add(handle);
    }

    public unsafe void SetUniformBufferSize(UniformGpuSlot slot, nuint capacity) =>
        _gl.BufferData(BufferTargetARB.UniformBuffer, capacity, (void*)0, BufferUsageARB.StaticDraw);


    public unsafe void UploadUbo<T>(in GfxHandle ubo, in T data, nuint offset, nuint size = 0)
        where T : unmanaged, IUniformGpuData
    {
        fixed (T* p = &data)
            _gl.BufferSubData(BufferTargetARB.UniformBuffer, (nint)offset, size, p);
    }

    public void BindUniformBufferRange(in GfxHandle ubo, UniformGpuSlot slot, nuint offset, nuint size)
    {
        var handle = _store.UboStore.Get(ubo).Handle;
        CheckGlError();
        _gl.BindBufferRange(BufferTargetARB.UniformBuffer, (uint)slot, handle, (nint)offset, size);
    }


    public GfxHandle CreateVertexArray(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementType drawElement,
        out MeshMeta meta)
    {
        var handle = _gl.GenVertexArray();
        meta = new MeshMeta(primitive, drawKind, drawElement, 0, 0);
        return _store.MeshStore.Add(new GlMeshHandle(handle));
    }

    public GfxHandle CreateVertexBuffer(BufferUsage usage, uint elementSize, uint bindingIndex,
        out VertexBufferMeta meta)
    {
        var handle = _gl.GenBuffer();
        meta = new VertexBufferMeta(usage, bindingIndex, 0, elementSize);
        return _store.VboStore.Add(new GlVboHandle(handle));
    }

    public GfxHandle CreateIndexBuffer(BufferUsage usage, uint elementSize, out IndexBufferMeta meta)
    {
        var handle = _gl.GenBuffer();
        meta = new IndexBufferMeta(usage, 0, elementSize);
        return _store.IboStore.Add(new GlIboHandle(handle));
    }

    public void BindFramebuffer(in GfxHandle fbo)
    {
        var handle = fbo == default ? 0 : _store.FboStore.Get(fbo).Handle;
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
    }


    public void BindFrameBufferReadDraw(in GfxHandle readFbo, in GfxHandle drawFbo)
    {
        var read = readFbo == default ? 0 : _store.FboStore.Get(readFbo).Handle;
        var draw = drawFbo == default ? 0 : _store.FboStore.Get(drawFbo).Handle;

        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, read);
        _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, draw);
    }

    public void CreateFramebuffer(in FrameBufferDesc desc, out DriverCreateFboResult result)
    {
        _fboFactory.CreateFrameBuffer(in desc, out var fbo, out var fboTex, out var rboTex,
            out var rboDepth);

        var fboHandle = _store.FboStore.Add(fbo.Handle);
        var texHandle = fboTex.Handle != default ? _store.TextureStore.Add(fboTex.Handle) : default;
        var rboDeptHandle = rboDepth.Handle != default ? _store.RboStore.Add(rboDepth.Handle) : default;
        var rboTexHandle = rboTex.Handle != default ? _store.RboStore.Add(rboTex.Handle) : default;
        CheckGlError();

        result = new DriverCreateFboResult(new DriverHandleMeta<FrameBufferMeta>(in fboHandle, fbo.Meta),
            new DriverHandleMeta<TextureMeta>(in texHandle, fboTex.Meta),
            new DriverHandleMeta<RenderBufferMeta>(in rboDeptHandle, rboDepth.Meta),
            new DriverHandleMeta<RenderBufferMeta>(in rboTexHandle, rboTex.Meta)
        );
    }

    public void Blit(Vector2D<int> srcSize, Vector2D<int> dstSize, bool linear)
    {
        _gl.BlitFramebuffer(
            0, 0, srcSize.X, srcSize.Y,
            0, 0, dstSize.X, dstSize.Y,
            ClearBufferMask.ColorBufferBit,
            linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest
        );
        CheckGlError();
    }

    public void BindVertexArray(in GfxHandle vao)
    {
        if (!vao.IsValid)
        {
            _gl.BindVertexArray(0);
            return;
        }
        _gl.BindVertexArray(_store.MeshStore.Get(vao).Handle);
    }

    public void BindVertexBuffer(in GfxHandle vbo)
    {
        if (!vbo.IsValid)
        {
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            return;
        }
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _store.VboStore.Get(vbo).Handle);
    }

    public void BindIndexBuffer(in GfxHandle ibo)
    {
        if (!ibo.IsValid)
        {
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
            return;
        }
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _store.IboStore.Get(ibo).Handle);
    }

    public void BindUniformBuffer(in GfxHandle ubo)
    {
        if (!ubo.IsValid)
        {
            _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
            return;
        }
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, _store.UboStore.Get(ubo).Handle);
    }


    public unsafe void SetVertexAttribute(in GfxHandle vao, uint index, in VertexAttributeDescriptor attribute)
    {
        var handle = _store.MeshStore.Get(vao).Handle;
        _gl.EnableVertexAttribArray(index);
        _gl.VertexAttribPointer(index, (int)attribute.Format, VertexAttribPointerType.Float, attribute.Normalized,
            attribute.StrideBytes,
            (void*)attribute.OffsetBytes);
        if (attribute.Divisor != 0) _gl.VertexAttribDivisor(index, attribute.Divisor);
        CheckGlError();
    }

    public void SetIndexBuffer<T>(in GfxHandle vao, in GfxHandle ibo, ReadOnlySpan<T> data, nuint size,
        BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged
    {
        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, size, data, usage.ToGlEnum());
        CheckGlError();
    }

    public void SetVertexBuffer<T>(in GfxHandle vao, in GfxHandle vbo, ReadOnlySpan<T> data, nuint size,
        BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged
    {
        _gl.BufferData(BufferTargetARB.ArrayBuffer, size, data, usage.ToGlEnum());
        CheckGlError();
    }

    public void UploadVertexBuffer<T>(in GfxHandle vbo, ReadOnlySpan<T> data, nuint offsetByte)
        where T : unmanaged
    {
        _gl.BufferSubData(BufferTargetARB.ArrayBuffer, (nint)offsetByte, data);
        CheckGlError();
    }
    //_gl.NamedBufferSubData(vbo.Handle, (nint)dstOffsetBytes, (nuint)(data.Length * Unsafe.SizeOf<T>()), data);

    public void UploadIndexBuffer<T>(in GfxHandle ibo, ReadOnlySpan<T> data, nuint offsetByte)
        where T : unmanaged
    {
        _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, (nint)offsetByte, data);
        CheckGlError();
    }
    // _gl.NamedBufferSubData(ibo.Handle, (nint)dstOffsetBytes, (nuint)(data.Length * Unsafe.SizeOf<T>()), data);


    // Textures
    public GfxHandle CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta)
    {
        var handle = _textureFactory.CreateTexture2D(data, in desc, out meta);
        return _store.TextureStore.Add(handle);
    }


    public GfxHandle CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta)
    {
        var handle = _textureFactory.CreateCubeMap(data, in desc, out meta);
        return _store.TextureStore.Add(handle);
    }


    public void BindTextureUnit(in GfxHandle tex, uint slot)
    {
        if (!tex.IsValid)
        {
            _gl.BindTextureUnit(slot, 0);
            return;

        }
        _gl.BindTextureUnit(slot, _store.TextureStore.Get(tex).Handle);
    }


    // Draw calls
    public void DrawArrays(DrawPrimitive primitive, uint drawCount)
    {
        _gl.DrawArrays(primitive.ToGlEnum(), 0, drawCount);
    }

    public unsafe void DrawElements(DrawPrimitive primitive, DrawElementType elementType, uint drawCount)
    {
        _gl.DrawElements(primitive.ToGlEnum(), drawCount, elementType.ToGlEnum(), (void*)0);
    }


    // Shader/Program
    public GfxHandle CreateShader(string vs, string fs, out ShaderLayout layout, out ShaderMeta meta)
    {
        var handle = _shaderFactory.CreateShader(vs, fs, out layout, out meta);
        return _store.ShaderStore.Add(handle);
    }

    public void UseShader(in GfxHandle shader)
    {
        if (!shader.IsValid)
        {
            _gl.UseProgram(0);
            return;

        }
        _gl.UseProgram(_store.ShaderStore.Get(shader).Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(int uniform, int value) => _gl.Uniform1(uniform, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(int uniform, uint value) => _gl.Uniform1(uniform, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(int uniform, float value) => _gl.Uniform1(uniform, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(int uniform, Vector2 value) => _gl.Uniform2(uniform, value.X, value.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(int uniform, Vector3 value) => _gl.Uniform3(uniform, value.X, value.Y, value.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(int uniform, Vector4 value) => _gl.Uniform4(uniform, value.X, value.Y, value.Z, value.W);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetUniform(int uniform, in Matrix4x4 value)
    {
        var p = (float*)Unsafe.AsPointer(ref Unsafe.AsRef(in value));
        _gl.UniformMatrix4(uniform, 1, false, p);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetUniform(int uniform, in Matrix3 value)
    {
        var p = (float*)Unsafe.AsPointer(ref Unsafe.AsRef(in value));
        _gl.UniformMatrix3(uniform, 1, false, p);
    }

    // Disposer
    public void DeleteGfxResource(GfxHandle handle)
    {
        switch (handle.Kind)
        {
            case ResourceKind.Texture: DisposeTexture(handle); break;
            case ResourceKind.Shader: DisposeShader(handle); break;
            case ResourceKind.Mesh: DisposeVao(handle); break;
            case ResourceKind.VertexBuffer: DisposeVbo(handle); break;
            case ResourceKind.IndexBuffer: DisposeIbo(handle); break;
            case ResourceKind.FrameBuffer: DisposeFbo(handle); break;
            case ResourceKind.RenderBuffer: DisposeRbo(handle); break;
            default: throw new ArgumentOutOfRangeException(nameof(handle), handle, $"Invalid resource {handle.Kind}");
        }
    }

    private void DisposeTexture(GfxHandle handle) => _gl.DeleteTexture(handle.Slot);
    private void DisposeShader(GfxHandle handle) => _gl.DeleteTexture(handle.Slot);
    private void DisposeVao(GfxHandle handle) => _gl.DeleteVertexArray(handle.Slot);
    private void DisposeVbo(GfxHandle handle) => _gl.DeleteBuffer(handle.Slot);
    private void DisposeIbo(GfxHandle handle) => _gl.DeleteBuffer(handle.Slot);
    private void DisposeFbo(GfxHandle handle) => _gl.DeleteFramebuffer(handle.Slot);
    private void DisposeRbo(GfxHandle handle) => _gl.DeleteRenderbuffer(handle.Slot);

    private void CheckGlError()
    {
        var error = _gl.GetError();
        if (error != (GLEnum)ErrorCode.NoError)
            throw new OpenGlException(error);
    }

    // Utils
    private static DeviceCapabilities CreateDeviceCapabilities(GL gl)
    {
        var maxTexUnits = gl.GetInteger(GLEnum.MaxCombinedTextureImageUnits);
        return new DeviceCapabilities
        {
            GlVersion = new OpenGlVersion(gl.GetInteger(GetPName.MajorVersion), gl.GetInteger(GetPName.MinorVersion)),
            MaxTextureImageUnits = gl.GetInteger(GLEnum.MaxCombinedTextureImageUnits),
            MaxVertexAttribBindings = gl.GetInteger((GLEnum)0x82DA), // GL_MAX_VERTEX_ATTRIB_BINDINGS
            MaxTextureSize = gl.GetInteger(GLEnum.MaxTextureSize),
            MaxArrayTextureLayers = gl.GetInteger(GLEnum.MaxArrayTextureLayers),
            MaxFramebufferWidth = gl.GetInteger((GLEnum)0x9315), // GL_MAX_FRAMEBUFFER_WIDTH
            MaxFramebufferHeight = gl.GetInteger((GLEnum)0x9316), // GL_MAX_FRAMEBUFFER_HEIGHT
            MaxSamples = gl.GetInteger(GLEnum.MaxSamples),
            MaxColorAttachments = gl.GetInteger(GLEnum.MaxColorAttachments),
            MaxAnisotropy = gl.GetFloat(GLEnum.MaxTextureMaxAnisotropy),
            MaxUniformBlockSize = gl.GetInteger(GLEnum.MaxUniformBlockSize),
            UniformBufferOffsetAlignment = gl.GetInteger(GetPName.UniformBufferOffsetAlignment),
        };
    }
}