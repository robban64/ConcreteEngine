using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics;


public interface IGraphicsContext
{
    GraphicsConfiguration Configuration { get; }
    DeviceCapabilities Capabilities { get; }

    //
    //void BeginFrame(in FrameInfo frameCtx);
    //void EndFrame(out GpuFrameStats result);

    void Clear(Color4 color, ClearBufferFlag flags);
    void SetBlendMode(BlendMode blendMode);
    void SetDepthMode(DepthMode depthMode);

    void SetCullMode(CullMode cullMode);
    void SetViewport(in Vector2D<int> viewport);


    void BeginScreenPass(Color4? clear = null, ClearBufferFlag? flags = null);
    void BeginRenderPass(in FrameBufferId fboId, Color4? clear, ClearBufferFlag? flags);
    void EndRenderPass();
    void BlitFramebuffer(in FrameBufferId fromId, in FrameBufferId toId = default, bool linear = true);

    void BindTexture(TextureId resourceId, uint slot);
    void BindMesh(MeshId id);
    void BindVertexBuffer(VertexBufferId id);
    void BindIndexBuffer(IndexBufferId id);
    void BindUniformBuffer(UniformGpuSlot slot);
    void BindFramebuffer(FrameBufferId id);


    void SetVertexAttribute(ReadOnlySpan<VertexAttributeDescriptor> attributes);
    void SetVertexBuffer<T>(ReadOnlySpan<T> data, BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged;
    void SetIndexBuffer<T>(ReadOnlySpan<T> data, BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged;

    void UploadVertexBuffer<T>(VertexBufferId vbo, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged;
    void UploadIndexBuffer<T>(IndexBufferId ibo, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged;

    void SetUniformBufferSize(UniformGpuSlot slot, nuint capacityBytes);
    void UploadUniformGpuData<T>(UniformGpuSlot slot, in T data, nuint offset = 0) where T : unmanaged, IUniformGpuData;
    void BindUniformBufferRange(UniformGpuSlot slot, nuint offset, nuint size);

    void DrawBoundMesh(uint drawCount = 0);
    void DrawArrays(DrawPrimitive primitive, uint drawCount);
    void DrawElements(DrawPrimitive primitive, DrawElementType elementType, uint drawCount);

    void UseShader(ShaderId id);
    void SetUniform(ShaderUniform uniform, int value);
    void SetUniform(ShaderUniform uniform, uint value);
    void SetUniform(ShaderUniform uniform, float value);
    void SetUniform(ShaderUniform uniform, Vector2 value);
    void SetUniform(ShaderUniform uniform, Vector3 value);
    void SetUniform(ShaderUniform uniform, Vector4 value);
    void SetUniform(ShaderUniform uniform, in Matrix4x4 value);
    void SetUniform(ShaderUniform uniform, in Matrix3 value);
}

internal interface IProgramContext;

internal interface IBufferContext;

internal interface IMeshContext;

internal interface ITextureContext;

internal interface IFramebufferContext;

internal interface IStateContext;