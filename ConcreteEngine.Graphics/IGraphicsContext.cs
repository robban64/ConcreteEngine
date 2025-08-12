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

    ViewTransform2D ViewTransform { get; }
    Vector2D<int> FramebufferSize { get; }
    Vector2D<int> ViewportSize { get; }

    void Begin(in RenderFrameContext frameCtx);
    void BeginRender();
    void End();
/*
    void BindRenderPass();
    void BindRenderPass(RenderPass renderPass, in Matrix4X4<float> projViewMatrix);
    void FlushRenderPass();
    void SubmitDraw(IDrawCommand cmd);

    */

    void SetBlendMode(BlendMode blendMode);
    void BindDefaultFramebuffer();
    void Clear(Color color);

    void UseShader(ushort resourceId);
    void BindTexture(ushort resourceId, uint slot);
    void BindMesh(ushort resourceId);
    void BindVertexBuffer(ushort resourceId);
    void BindIndexBuffer(ushort resourceId);

    void SetVertexBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged;
    void SetIndexBuffer(ReadOnlySpan<uint> data);

    void UploadVertexBuffer<T>(ReadOnlySpan<T> data, int offsetElements) where T : unmanaged;
    void UploadIndexBuffer(ReadOnlySpan<uint> data, int offsetElements);

    void Draw(uint drawCount = 0);
    void DrawIndexed(uint drawCount = 0);

    void SetUniform(ShaderUniform uniform, int value);

    void SetUniform(ShaderUniform uniform, uint value);

    void SetUniform(ShaderUniform uniform, float value);

    void SetUniform(ShaderUniform uniform, Vector2D<float> value);

    void SetUniform(ShaderUniform uniform, Vector3D<float> value);

    void SetUniform(ShaderUniform uniform, Vector4D<float> value);

    void SetUniform(ShaderUniform uniform, in Matrix4X4<float> value);

    IRenderTarget CreateRenderTarget();
}