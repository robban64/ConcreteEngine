#region

using System.Drawing;
using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics;

public interface IGraphicsContextOld
{
    GraphicsConfiguration Configuration { get; }
    DeviceCapabilities  Capabilities { get; }
    
    //

    void BeginFrame(in FrameInfo frameCtx);
    void EndFrame(out GpuFrameStats result);
    
    void Clear(Color4 color, ClearBufferFlag flags);
    void SetBlendMode(BlendMode blendMode);
    void SetDepthMode(DepthMode depthMode);
    void SetCullMode(CullMode cullMode);
    void SetViewport(in Vector2D<int> viewport);


    void BeginScreenPass(Color4? clear = null, ClearBufferFlag? flags = null);
    void BeginRenderPass(FrameBufferId fboId, Color4? clear, ClearBufferFlag? flags);
    void EndRenderPass();
    void BlitFramebuffer(FrameBufferId fromId, FrameBufferId toId = default, bool linearFilter = true);

    void BindTexture(TextureId resourceId, uint slot);
    void BindMesh(MeshId resourceId);
    void BindVertexBuffer(VertexBufferId resourceId);
    void BindIndexBuffer(IndexBufferId resourceId);
    void BindUniformBuffer(UniformGpuSlot slot);
    void BindFramebuffer(FrameBufferId id);

    void SetVertexAttribute(ReadOnlySpan<VertexAttributeDescriptor> attributes);
    void SetVertexBuffer<T>(ReadOnlySpan<T> data, BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged;
    void SetIndexBuffer<T>(ReadOnlySpan<T> data, BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged;

    void UploadVertexBuffer<T>(ReadOnlySpan<T> data, int offsetElements) where T : unmanaged;
    void UploadIndexBuffer<T>(ReadOnlySpan<T> data, int offsetElements) where T : unmanaged;

    void SetUniformBufferSize(UniformGpuSlot slot, nuint capacityBytes);
    void UploadUniformGpuData<T>(UniformGpuSlot slot, in T data, nuint offset = 0) where T : unmanaged, IUniformGpuData;
    void BindUniformBufferRange(UniformGpuSlot slot, nuint offset, nuint size);
    
    void DrawMesh(uint drawCount = 0);

    void UseShader(ShaderId resourceId);
    void SetUniform(ShaderUniform uniform, int value);
    void SetUniform(ShaderUniform uniform, uint value);
    void SetUniform(ShaderUniform uniform, float value);
    void SetUniform(ShaderUniform uniform, Vector2 value);
    void SetUniform(ShaderUniform uniform, Vector3 value);
    void SetUniform(ShaderUniform uniform, Vector4 value);
    void SetUniform(ShaderUniform uniform, in Matrix4x4 value);
    void SetUniform(ShaderUniform uniform, in Matrix3 value);
}