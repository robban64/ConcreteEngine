#region

using System.Drawing;
using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics;

public interface IGraphicsContext
{
    GraphicsConfiguration Configuration { get; }

    Vector2D<int> ViewportSize { get; }
    BlendMode BlendMode { get; }
    bool DepthTest { get; }

    void BeginFrame(in FrameMetaInfo frameCtx);
    void EndFrame(out FrameRenderResult result);

    void Clear(Color4 color, ClearBufferFlag flags);
    void BeginScreenPass(Color4? clear = null, ClearBufferFlag? flags = null);
    void BeginRenderPass(FrameBufferId fboId, Color4? clear, ClearBufferFlag? flags);
    void EndRenderPass();
    void BlitFramebuffer(FrameBufferId fromId, FrameBufferId toId = default, bool linearFilter = true);
    void SetBlendMode(BlendMode blendMode);
    void SetDepthTest(bool depthTest);
    void BindTexture(TextureId resourceId, uint slot);
    void BindMesh(MeshId resourceId);
    void BindVertexBuffer(VertexBufferId resourceId);
    void BindIndexBuffer(IndexBufferId resourceId);

    void SetVertexBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged;
    void SetIndexBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged;

    void UploadVertexBuffer<T>(ReadOnlySpan<T> data, int offsetElements) where T : unmanaged;
    void UploadIndexBuffer<T>(ReadOnlySpan<T> data, int offsetElements) where T : unmanaged;

    void DrawMesh(uint drawCount = 0);

    void UseShader(ShaderId resourceId);
    void SetUniform(ShaderUniform uniform, int value);
    void SetUniform(ShaderUniform uniform, uint value);
    void SetUniform(ShaderUniform uniform, float value);
    void SetUniform(ShaderUniform uniform, Vector2 value);
    void SetUniform(ShaderUniform uniform, Vector3 value);
    void SetUniform(ShaderUniform uniform, Vector4 value);
    void SetUniform(ShaderUniform uniform, in Matrix4x4 value);
}