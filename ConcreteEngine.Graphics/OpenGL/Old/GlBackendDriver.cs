/*
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
       private readonly GL _gl;
       private readonly GlCtx _ctx;
   
       private readonly ResourceBackendDispatcher _dispatcher;
       private readonly BackendOpsHub _store;
   
       private readonly GlCapabilities _capabilities;
       private readonly GlDebugger _debugger;
       private readonly GlDisposer _disposer;
       private readonly GlBuffers _buffers;
       private readonly GlTextures _textures;
       private readonly GlMeshes _meshes;
       private readonly GlShaders _shaders;
       private readonly GlStates _states;
       private readonly GlFrameBuffers _frameBuffers;
   
   
       private readonly GraphicsConfiguration _configuration;
   
       private GlTextureFactory _textureFactory = null!;
       private GlShaderFactory _shaderFactory = null!;
       private GlFboFactory _fboFactory = null!;
   
       public GraphicsConfiguration Configuration => _configuration;
   
       public DeviceCapabilities Capabilities => _capabilities.Caps;
   
       internal GlBackendDriver(GlStartupConfig config, BackendOpsHub store, ResourceBackendDispatcher dispatcher)
       {
           _gl = config.DriverContext;
           _store = store;
           _dispatcher = dispatcher;
           _capabilities = new GlCapabilities();
           _configuration = new GraphicsConfiguration();
   
           _ctx = new GlCtx { Capabilities = _capabilities, Gl = _gl, Store = _store, Dispatcher = _dispatcher };
   
           _debugger = new GlDebugger(_gl);
           _disposer = new GlDisposer(_ctx);
           _buffers = new GlBuffers(_ctx);
           _textures = new GlTextures(_ctx);
           _meshes = new GlMeshes(_ctx);
           _shaders = new GlShaders(_ctx);
           _states = new GlStates(_ctx);
           _frameBuffers = new GlFrameBuffers(_ctx);
   
       }
   
       internal void Initialize()
       {
           Console.WriteLine($"OpenGL version {Capabilities.GlVersion} loaded.");
           Console.WriteLine("--Device Capability--");
           Console.WriteLine(Capabilities.ToString());
           
           _debugger.EnableGlDebug();
   
           _gl.Enable(GLEnum.Dither);
           _gl.Enable(GLEnum.Multisample);
           _gl.Enable(EnableCap.TextureCubeMapSeamless);
           _gl.PixelStore(GLEnum.UnpackAlignment, 1);
   
           _gl.DepthMask(true);
   
           _gl.Enable(EnableCap.CullFace);
           _gl.CullFace(TriangleFace.Back);
           _gl.FrontFace(FrontFaceDirection.Ccw);
       }
   
   
       public void PrepareFrame()
       {
       }
   
       public void ValidateEndFrame()
       {
           _debugger.CheckGlError();
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
   
       public unsafe void SetUniformBufferSize(UniformGpuSlot slot, nuint capacity) =>
           _gl.BufferData(BufferTargetARB.UniformBuffer, capacity, (void*)0, BufferUsageARB.DynamicDraw);
   
   
       public unsafe void UploadUniformBuffer<T>(in GfxHandle ubo, in T data, nuint offset, nuint size)
           where T : unmanaged, IUniformGpuData
       {
           fixed (T* p = &data)
               _gl.BufferSubData(BufferTargetARB.UniformBuffer, (nint)offset, size, p);
       }
   
       public void BindUniformBufferRange(in GfxHandle ubo, UniformGpuSlot slot, nuint offset, nuint size)
       {
           var handle = _store.UniformBuffer.Get(ubo).Handle;
           _gl.BindBufferRange(BufferTargetARB.UniformBuffer, (uint)slot, handle, (nint)offset, size);
       }
   
   
       public void BindFrameBuffer(in GfxHandle fbo)
       {
           var handle = !fbo.IsValid ? 0 : _store.FrameBuffer.Get(fbo).Handle;
           _gl.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
           if (handle != 0)
           {
               _gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
               _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
           }
       }
   
       public void BindRenderBuffer(in GfxHandle rbo)
       {
           var handle = !rbo.IsValid ? 0 : _store.FrameBuffer.Get(rbo).Handle;
           _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, handle);
       }
   
   
       public void BindFrameBufferReadDraw(in GfxHandle readFbo, in GfxHandle drawFbo)
       {
           var read = !readFbo.IsValid ? 0 : _store.FrameBuffer.Get(readFbo).Handle;
           var draw = !drawFbo.IsValid ? 0 : _store.FrameBuffer.Get(drawFbo).Handle;
   
           _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, read);
           _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, draw);
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
   
       public void BindVertexArray(in GfxHandle vao)
       {
           if (!vao.IsValid)
           {
               _gl.BindVertexArray(0);
               return;
           }
   
           _gl.BindVertexArray(_store.VertexArray.Get(vao).Handle);
       }
   
       public void BindVertexBuffer(in GfxHandle vbo)
       {
           if (!vbo.IsValid)
           {
               _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
               return;
           }
   
           _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _store.VertexBuffer.Get(vbo).Handle);
       }
   
       public void BindIndexBuffer(in GfxHandle ibo)
       {
           if (!ibo.IsValid)
           {
               _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
               return;
           }
   
           _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _store.IndexBuffer.Get(ibo).Handle);
       }
   
       public void BindUniformBuffer(in GfxHandle ubo)
       {
           if (!ubo.IsValid)
           {
               _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
               return;
           }
   
           _gl.BindBuffer(BufferTargetARB.UniformBuffer, _store.UniformBuffer.Get(ubo).Handle);
       }
   
   
       public unsafe void SetVertexAttribute(in GfxHandle vao, uint index, in VertexAttributeDescriptor attribute)
       {
           var handle = _store.VertexArray.Get(vao).Handle;
           _gl.EnableVertexAttribArray(index);
           _gl.VertexAttribPointer(index, (int)attribute.Format, VertexAttribPointerType.Float, attribute.Normalized,
               attribute.StrideBytes,
               (void*)attribute.OffsetBytes);
           if (attribute.Divisor != 0) _gl.VertexAttribDivisor(index, attribute.Divisor);
       }
   
       public void SetIndexBuffer<T>(in GfxHandle vao, in GfxHandle ibo, ReadOnlySpan<T> data, nuint size,
           BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged
       {
           _gl.BufferData(BufferTargetARB.ElementArrayBuffer, size, data, usage.ToGlEnum());
       }
   
       public void SetVertexBuffer<T>(in GfxHandle vao, in GfxHandle vbo, ReadOnlySpan<T> data, nuint size,
           BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged
       {
           _gl.BufferData(BufferTargetARB.ArrayBuffer, size, data, usage.ToGlEnum());
       }
   
       public void UploadVertexBuffer<T>(in GfxHandle vbo, ReadOnlySpan<T> data, nuint offsetByte)
           where T : unmanaged
       {
           _gl.BufferSubData(BufferTargetARB.ArrayBuffer, (nint)offsetByte, data);
       }
       //_gl.NamedBufferSubData(vbo.Handle, (nint)offsetByte, (nuint)(data.Length * Unsafe.SizeOf<T>()), data);
   
       public void UploadIndexBuffer<T>(in GfxHandle ibo, ReadOnlySpan<T> data, nuint offsetByte)
           where T : unmanaged
       {
           _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, (nint)offsetByte, data);
       }
       // _gl.NamedBufferSubData(ibo.Handle, (nint)offsetByte, (nuint)(data.Length * Unsafe.SizeOf<T>()), data);
   
       public void BindTextureUnit(in GfxHandle tex, uint slot)
       {
           if (!tex.IsValid)
           {
               _gl.BindTextureUnit(slot, 0);
               return;
           }
   
           _gl.BindTextureUnit(slot, _store.Texture.Get(tex).Handle);
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
       public ResourceRefToken<ShaderId> CreateShader(string vs, string fs, out List<(string, int)> uniforms,
           out ShaderMeta meta)
       {
           var handle = _shaderFactory.CreateShader(vs, fs, out uniforms, out meta);
           return _store.Shader.Add(handle);
       }
   
       public void UseShader(in GfxHandle shader)
       {
           if (!shader.IsValid)
           {
               _gl.UseProgram(0);
               return;
           }
   
           _gl.UseProgram(_store.Shader.Get(shader).Handle);
       }
   
       public Dictionary<string, int> GetUniforms()
       {
           throw new NotImplementedException();
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
       public void DeleteGfxResource(in DeleteCmd cmd)
       {
           switch (cmd.Handle.Kind)
           {
               case ResourceKind.Texture:
                   DisposeTexture(in cmd);
                   break;
               case ResourceKind.Shader:
                   DisposeShader(in cmd);
                   break;
               case ResourceKind.Mesh:
                   DisposeVao(in cmd);
                   break;
               case ResourceKind.VertexBuffer:
                   DisposeVbo(in cmd);
                   break;
               case ResourceKind.IndexBuffer:
                   DisposeIbo(in cmd);
                   break;
               case ResourceKind.FrameBuffer:
                   DisposeFbo(in cmd);
                   break;
               case ResourceKind.RenderBuffer:
                   DisposeRbo(in cmd);
                   break;
               default: throw new ArgumentOutOfRangeException(nameof(cmd), cmd, $"Invalid resource {cmd.Handle.Kind}");
           }
       }
   
       private void DisposeTexture(in DeleteCmd cmd)
       {
           _gl.DeleteTexture(cmd.NativeHandle.Value);
           _dispatcher.OnDelete(in cmd);
       }
   
       private void DisposeShader(in DeleteCmd cmd)
       {
           _gl.DeleteProgram(cmd.NativeHandle.Value);
           _dispatcher.OnDelete(in cmd);
       }
   
       private void DisposeVao(in DeleteCmd cmd)
       {
           _gl.DeleteVertexArray(cmd.NativeHandle.Value);
           _dispatcher.OnDelete(in cmd);
       }
   
       private void DisposeVbo(in DeleteCmd cmd)
       {
           _gl.DeleteBuffer(cmd.NativeHandle.Value);
           _dispatcher.OnDelete(in cmd);
       }
   
       private void DisposeIbo(in DeleteCmd cmd)
       {
           _gl.DeleteBuffer(cmd.NativeHandle.Value);
           _dispatcher.OnDelete(in cmd);
       }
   
       private void DisposeFbo(in DeleteCmd cmd)
       {
           _gl.DeleteFramebuffer(cmd.NativeHandle.Value);
           _dispatcher.OnDelete(in cmd);
       }
   
       private void DisposeRbo(in DeleteCmd cmd)
       {
           _gl.DeleteRenderbuffer(cmd.NativeHandle.Value);
           _dispatcher.OnDelete(in cmd);
       }
   
   
       // Utils
   }
   */