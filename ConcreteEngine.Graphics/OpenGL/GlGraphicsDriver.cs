using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal class BackendDriverStores
{
    public readonly DriverResourceStore<GlTextureHandle> TextureStore = new(GraphicsBackend.OpenGL,
        ResourceKind.Texture);

    public readonly DriverResourceStore<GlShaderHandle> ShaderStore = new(GraphicsBackend.OpenGL, ResourceKind.Shader);
    public readonly DriverResourceStore<GlMeshHandle> MeshStore = new(GraphicsBackend.OpenGL, ResourceKind.Mesh);
    public readonly DriverResourceStore<GlVboHandle> VboStore = new(GraphicsBackend.OpenGL, ResourceKind.VertexBuffer);
    public readonly DriverResourceStore<GlIboHandle> IboStore = new(GraphicsBackend.OpenGL, ResourceKind.IndexBuffer);
    public readonly DriverResourceStore<GlFboHandle> FboStore = new(GraphicsBackend.OpenGL, ResourceKind.FrameBuffer);
    public readonly DriverResourceStore<GlRboHandle> RboStore = new(GraphicsBackend.OpenGL, ResourceKind.RenderBuffer);
    public readonly DriverResourceStore<GlUboHandle> UboStore = new(GraphicsBackend.OpenGL, ResourceKind.UniformBuffer);
}

internal sealed class GlBackendDriver : IGraphicsDriver
{
    private readonly GL _gl;
    private readonly int _glMinor = 0;
    private readonly int _glMajor = 0;

    private readonly GlTextureFactory _textureFactory;
    private readonly GlShaderFactory _shaderFactory;
    private readonly GlFboFactory _fboFactory;

    private FrameInfo _frameCtx;

    public GraphicsBackend BackendApi { get; }
    public GraphicsConfiguration Configuration { get; }
    public DeviceCapabilities Capabilities { get; }

    private readonly BackendDriverStores _store;


    internal GlBackendDriver(GL gl)
    {
        _gl = gl;
        Capabilities = CreateDeviceCapabilities(gl);
        Configuration = new GraphicsConfiguration();

        _store = new BackendDriverStores();

        _textureFactory = new GlTextureFactory(_gl, Capabilities);
        _shaderFactory = new GlShaderFactory(_gl, Capabilities);
        _fboFactory = new GlFboFactory(_gl, Capabilities, _textureFactory);

        Console.WriteLine($"OpenGL version {Capabilities.GlVersion} loaded.");
        Console.WriteLine("--Device Capability--");
        Console.WriteLine(Capabilities.ToString());

        _gl.GetInteger(GetPName.MajorVersion, out _glMajor);
        _gl.GetInteger(GetPName.MinorVersion, out _glMinor);
        int glVersion = _glMajor * 100 + _glMinor * 10;

        _gl.Disable(GLEnum.CullFace);
        _gl.Enable(GLEnum.Dither);
        _gl.Enable(GLEnum.Multisample);
        _gl.PixelStore(GLEnum.UnpackAlignment, 1);
        _gl.Enable(EnableCap.TextureCubeMapSeamless);
    }

    public void PrepareFrame(in FrameInfo frameCtx)
    {
        _frameCtx = frameCtx;
        _gl.UseProgram(0);
    }

    public void Clear(Color4 color, ClearBufferFlag flags)
    {
        _gl.ClearColor(color.R, color.G, color.B, 1);
        _gl.Clear(flags.ToGlEnum());
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
        return _store.MeshStore.Add(new GlMeshHandle(handle));
    }

    public GfxHandle CreateIndexBuffer(BufferUsage usage, uint elementSize, out IndexBufferMeta meta)
    {
        var handle = _gl.GenBuffer();
        meta = new IndexBufferMeta(usage, 0, elementSize);
        return _store.MeshStore.Add(new GlMeshHandle(handle));
    }

    public void BindFramebuffer(in GfxHandle fbo) =>
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _store.FboStore.Get(fbo).Handle);


    public void BindFrameBufferReadDraw(in GfxHandle readFbo, in GfxHandle drawFbo)
    {
        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _store.FboStore.Get(readFbo).Handle);
        _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _store.FboStore.Get(readFbo).Handle);
    }

    public void CreateFramebuffer(in FrameBufferDesc desc, out FboCreatedResult result)
    {
        _fboFactory.CreateFrameBuffer(_frameCtx.OutputSize, in desc, out var fboRes);
        result = default;
        _store.FboStore.Add(fboRes.Fbo.Fbo);
        if (fboRes.FboTex.Tex != default) _store.TextureStore.Add(fboRes.FboTex.Tex);
        if (fboRes.RboTex.Rbo != default) _store.RboStore.Add(fboRes.RboTex.Rbo);
        if (fboRes.RboDepth.Rbo != default) _store.RboStore.Add(fboRes.RboDepth.Rbo);
    }

    public void Blit(Vector2D<int> srcSize, Vector2D<int> dstSize, bool linear)
    {
        _gl.BlitFramebuffer(
            0, 0, srcSize.X, srcSize.Y,
            0, 0, dstSize.X, dstSize.Y,
            ClearBufferMask.ColorBufferBit,
            linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest
        );
    }

    public void BindVertexArray(in GfxHandle vao) => _gl.BindVertexArray(_store.MeshStore.Get(vao).Handle);

    public void BindVertexBuffer(in GfxHandle vbo) =>
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _store.VboStore.Get(vbo).Handle);

    public void BindIndexBuffer(in GfxHandle ibo) =>
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _store.IboStore.Get(ibo).Handle);

    public void BindUniformBuffer(in GfxHandle ubo) =>
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, _store.UboStore.Get(ubo).Handle);
    

    public unsafe void SetVertexAttribute(in GfxHandle vao, uint index, in VertexAttributeDescriptor attribute)
    {
        var handle = _store.MeshStore.Get(vao).Handle;
        _gl.EnableVertexAttribArray(index);
        _gl.VertexAttribPointer(index, (int)attribute.Format, VertexAttribPointerType.Float, attribute.Normalized,
            attribute.StrideBytes,
            (void*)attribute.OffsetBytes);
        if (attribute.Divisor != 0) _gl.VertexAttribDivisor(index, attribute.Divisor);
    }

    public void SetIndexBuffer<T>(in GfxHandle vao, in GfxHandle ibo, ReadOnlySpan<T> data, nuint size,
        BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged =>
        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, size, data, usage.ToGlEnum());

    public void SetVertexBuffer<T>(in GfxHandle vao, in GfxHandle vbo, ReadOnlySpan<T> data, nuint size,
        BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged =>
        _gl.BufferData(BufferTargetARB.ArrayBuffer, size, data, usage.ToGlEnum());

    public void UploadVertexBuffer<T>(in GfxHandle vbo, ReadOnlySpan<T> data, nuint offsetByte)
        where T : unmanaged =>
        _gl.BufferSubData(BufferTargetARB.ArrayBuffer, (nint)offsetByte, data);
    //_gl.NamedBufferSubData(vbo.Handle, (nint)dstOffsetBytes, (nuint)(data.Length * Unsafe.SizeOf<T>()), data);

    public void UploadIndexBuffer<T>(in GfxHandle ibo, ReadOnlySpan<T> data, nuint offsetByte)
        where T : unmanaged =>
        _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, (nint)offsetByte, data);
    // _gl.NamedBufferSubData(ibo.Handle, (nint)dstOffsetBytes, (nuint)(data.Length * Unsafe.SizeOf<T>()), data);


    public void DrawArrays(DrawPrimitive primitive, uint drawCount) =>
        _gl.DrawArrays(primitive.ToGlEnum(), 0, drawCount);

    public unsafe void DrawElements(DrawPrimitive primitive, uint drawCount, DrawElementType elementType) =>
        _gl.DrawElements(primitive.ToGlEnum(), drawCount, elementType.ToGlEnum(), (void*)0);

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


    public void BindTextureUnit(in GfxHandle tex, uint slot) =>
        _gl.BindTextureUnit(slot, _store.TextureStore.Get(tex).Handle);


    // Shader
    public GfxHandle CreateShader(string vs, string fs, out ShaderLayout layout, out ShaderMeta meta)
    {
        var handle = _shaderFactory.CreateShader(vs, fs, out layout, out meta);
        return _store.ShaderStore.Add(handle);
    }

    public void UseShader(in GfxHandle prog) => _gl.UseProgram(_store.ShaderStore.Get(prog).Handle);

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


    // Utils
    private static DeviceCapabilities CreateDeviceCapabilities(GL gl)
    {
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