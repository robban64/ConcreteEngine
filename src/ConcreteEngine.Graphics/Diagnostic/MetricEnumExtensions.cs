using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Specs.Graphics;

namespace ConcreteEngine.Graphics.Diagnostic;

public static class MetricEnumExtensions
{
    public static LogTopic ToLogTopic(this GraphicsHandleKind kind)
    {
        return kind switch
        {
            GraphicsHandleKind.Texture => LogTopic.Texture,
            GraphicsHandleKind.Shader => LogTopic.Shader,
            GraphicsHandleKind.Mesh => LogTopic.Mesh,
            GraphicsHandleKind.VertexBuffer => LogTopic.VertexBuffer,
            GraphicsHandleKind.IndexBuffer => LogTopic.IndexBuffer,
            GraphicsHandleKind.UniformBuffer => LogTopic.UniformBuffer,
            GraphicsHandleKind.FrameBuffer => LogTopic.FrameBuffer,
            GraphicsHandleKind.RenderBuffer => LogTopic.RenderBuffer,
            GraphicsHandleKind.Invalid => LogTopic.Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    public static GraphicsHandleKind ToResourceKind(this LogTopic topic)
    {
        return topic switch
        {
            LogTopic.Texture => GraphicsHandleKind.Texture,
            LogTopic.Shader => GraphicsHandleKind.Shader,
            LogTopic.Mesh => GraphicsHandleKind.Mesh,
            LogTopic.VertexBuffer => GraphicsHandleKind.VertexBuffer,
            LogTopic.IndexBuffer => GraphicsHandleKind.IndexBuffer,
            LogTopic.UniformBuffer => GraphicsHandleKind.UniformBuffer,
            LogTopic.FrameBuffer => GraphicsHandleKind.FrameBuffer,
            LogTopic.RenderBuffer => GraphicsHandleKind.RenderBuffer,
            LogTopic.Unknown => GraphicsHandleKind.Invalid,
            _ => throw new ArgumentOutOfRangeException(nameof(topic), topic, null)
        };
    }

}