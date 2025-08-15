#region

using System.Drawing;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics;

public interface IGraphicsContext
{
    GraphicsConfiguration Configuration { get; }
    ushort QuadMeshId { get; }

    Vector2D<int> ViewportSize { get; }

    void BeginFrame(in GraphicsFrameContext frameCtx);
    void EndFrame();
    void Clear(Color color, ClearBufferFlag flags);
    void BeginScreenPass(Color? clear = null, ClearBufferFlag flags = ClearBufferFlag.Color);
    void BeginRenderPass(ushort fboId, Color? clear, ClearBufferFlag flags = ClearBufferFlag.Color);
    void EndRenderPass();
    void BlitFramebufferTo(ushort fromId, ushort toId = 0, Vector2D<int>? size = null, bool linearFilter = true);
    void SetBlendMode(BlendMode blendMode);
    void BindFramebufferTexture(ushort framebufferId);
    void BindFramebuffer(ushort resourceId);
    void BindTexture(ushort resourceId, uint slot);
    void BindMesh(ushort resourceId);
    void BindVertexBuffer(ushort resourceId);
    void BindIndexBuffer(ushort resourceId);

    void SetVertexBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged;
    void SetIndexBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged;

    void UploadVertexBuffer<T>(ReadOnlySpan<T> data, int offsetElements) where T : unmanaged;
    void UploadIndexBuffer<T>(ReadOnlySpan<T> data, int offsetElements) where T : unmanaged;

    void Draw(uint drawCount = 0);
    void DrawIndexed(uint drawCount = 0);
    void DrawFboScreenQuad(ushort fboId, ushort shaderId);
    
    void UseShader(ushort resourceId);
    void SetUniform(ShaderUniform uniform, int value);
    void SetUniform(ShaderUniform uniform, uint value);
    void SetUniform(ShaderUniform uniform, float value);
    void SetUniform(ShaderUniform uniform, Vector2D<float> value);
    void SetUniform(ShaderUniform uniform, Vector3D<float> value);
    void SetUniform(ShaderUniform uniform, Vector4D<float> value);
    void SetUniform(ShaderUniform uniform, in Matrix4X4<float> value);
}